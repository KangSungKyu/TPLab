"""Package-contract tests using disposable repositories; never edits the development checkout."""
import hashlib
import importlib.util
import json
import shutil
import subprocess
import tarfile
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('distribution', ROOT / 'tools/build_distribution.py')
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], stderr=subprocess.STDOUT).decode().strip()


class DistributionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = tempfile.TemporaryDirectory(prefix='tplab-packaging-tests-')
        cls.template = Path(cls.base.name) / 'template'
        cls.template.mkdir()
        roots = ('Assets/TPLab/Core', 'Assets/TPLab/Input/Runtime', 'Assets/TPLab/Editor',
                 'Assets/Plugins/CsvHelper', 'doc/api', 'doc/ai/api', 'doc/licenses')
        exact = {'Assets/TPLab/Core.meta', 'Assets/TPLab/Input/Runtime.meta',
                 'Assets/TPLab/Editor.meta', 'Assets/Plugins/CsvHelper.meta',
                 'LICENSE', 'THIRD_PARTY_NOTICES.md', 'ProjectSettings/ProjectVersion.txt', '.gitattributes'}
        for path in git(ROOT, 'ls-files').splitlines():
            if path in exact or any(path.startswith(prefix + '/') for prefix in roots):
                target = cls.template / path
                target.parent.mkdir(parents=True, exist_ok=True)
                data = (ROOT / path).read_bytes()
                target.write_bytes(data if path.endswith('.dll') else data.replace(b'\r\n', b'\n'))
        (cls.template / '.gitignore').write_text('/tplab/\n', encoding='utf-8')
        git(cls.template, 'init', '-q')
        git(cls.template, 'config', 'user.name', 'TPLab fixture')
        git(cls.template, 'config', 'user.email', 'fixture@example.invalid')
        git(cls.template, 'config', 'core.autocrlf', 'false')
        git(cls.template, 'add', '.')
        git(cls.template, 'commit', '-qm', 'fixture')

    @classmethod
    def tearDownClass(cls):
        cls.base.cleanup()

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='tplab-package-case-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / 'source'
        subprocess.run(['git', 'clone', '-q', '--shared', str(self.template), str(self.root)], check=True)
        git(self.root, 'config', 'core.autocrlf', 'false')
        git(self.root, 'config', 'user.name', 'TPLab fixture')
        git(self.root, 'config', 'user.email', 'fixture@example.invalid')
        self.revision = git(self.root, 'rev-parse', 'HEAD')

    def build(self, run='first', prepare=True, **changes):
        args = dict(source=self.root, revision=self.revision, version='0.0.1', run_id=run,
                    output=self.root / 'tplab' / run, prepare=prepare)
        args.update(changes)
        return builder.build(**args)

    def commit(self):
        git(self.root, 'add', '.')
        git(self.root, 'commit', '-qm', 'case')
        self.revision = git(self.root, 'rev-parse', 'HEAD')

    def stage_snapshot(self):
        self.build()
        shutil.copytree(self.root / 'tplab/first/packages', self.root / 'upm')
        self.commit()

    def test_packages_docs_licenses_payload(self):
        self.build()
        folder = self.root / 'tplab/first'
        manifest = json.loads((folder / 'artifacts/distribution-manifest.json').read_text())
        self.assertEqual(3, len(manifest['packages']))
        self.assertFalse(manifest['publishable'])
        self.assertEqual(self.revision, manifest['sourceRevision'])
        for package in manifest['packages']:
            tree = folder / 'packages' / package['id']
            self.assertTrue((tree / 'README.md').is_file())
            self.assertTrue((tree / 'Documentation~/ai/README.md').is_file())
            self.assertEqual(subprocess.check_output(['git', '-C', str(self.root), 'show', 'HEAD:LICENSE']), (tree / 'LICENSE.md').read_bytes())
            archive = folder / 'artifacts' / package['filename']
            self.assertEqual(package['sha256'], hashlib.sha256(archive.read_bytes()).hexdigest())
            self.assertEqual(b'\x00' * 4, archive.read_bytes()[4:8])
            for src, dst in builder.PACKAGES[package['id']]['roots'].items():
                self.assertEqual((self.root / (src + '.meta')).read_bytes(), (tree / (dst + '.meta')).read_bytes())
            with tarfile.open(archive) as tar:
                for member in tar.getmembers():
                    self.assertTrue(member.name.startswith('package/'))
                    self.assertTrue(member.isfile())
                    self.assertEqual(0, member.mtime)
                    self.assertEqual(0, member.uid)
                    self.assertEqual(0o644, member.mode)
                    self.assertEqual((tree / member.name.removeprefix('package/')).read_bytes(), tar.extractfile(member).read())
        core = folder / 'packages/com.tplab.core'
        self.assertEqual((self.root / 'Assets/Plugins/CsvHelper/CsvHelper.dll').read_bytes(), (core / 'Runtime/ThirdParty/CsvHelper/CsvHelper.dll').read_bytes())
        self.assertTrue((core / 'Runtime/ThirdParty.meta').is_file())
        self.assertTrue((core / 'Documentation~/api/Pooling.md').is_file())
        self.assertTrue((core / 'Documentation~/ai/api/Pooling.md').is_file())
        self.assertFalse((core / 'Documentation~/api/Input.md').exists())
        self.assertNotIn('Temp/', (core / 'README.md').read_text(encoding='utf-8'))

    def test_same_input_reproducible_despite_run(self):
        self.build('one'); self.build('two')
        for a in (self.root / 'tplab/one/artifacts').glob('*.tgz'):
            self.assertEqual(a.read_bytes(), (self.root / 'tplab/two/artifacts' / a.name).read_bytes())

    def test_snapshot_matches_after_commit_without_hash_recursion(self):
        self.stage_snapshot(); self.build('verified', prepare=False)
        report = json.loads((self.root / 'tplab/verified/artifacts/distribution-manifest.json').read_text())
        self.assertEqual('Matched', report['publicationSnapshot'])
        self.assertFalse(report['publishable'])

    def test_snapshot_drift_refused(self):
        self.stage_snapshot()
        (self.root / 'upm/com.tplab.core/README.md').write_text('drift', encoding='utf-8')
        self.commit()
        with self.assertRaisesRegex(ValueError, 'snapshot'): self.build('drift', prepare=False)
        self.assertFalse((self.root / 'tplab/drift').exists())

    def test_missing_snapshot_refused(self):
        with self.assertRaisesRegex(ValueError, 'snapshot'): self.build(prepare=False)

    def test_dirty_refused_before_output(self):
        with (self.root / 'LICENSE').open('a') as f: f.write('dirty')
        with self.assertRaisesRegex(ValueError, 'clean'): self.build()
        self.assertFalse((self.root / 'tplab/first').exists())

    def test_untracked_refused(self):
        (self.root / 'unknown.txt').write_text('untracked')
        with self.assertRaisesRegex(ValueError, 'clean'): self.build()

    def test_wrong_revision_refused(self):
        with self.assertRaisesRegex(ValueError, 'revision'): self.build(revision='0' * 40)

    def test_abbreviated_revision_refused(self):
        with self.assertRaisesRegex(ValueError, 'revision'): self.build(revision=self.revision[:7])

    def test_invalid_version_refused(self):
        for version in ('../bad', '00.0.1', 'v0.0.1', '0.0', '0.0.1/escape'):
            with self.subTest(version=version), self.assertRaisesRegex(ValueError, 'version'): self.build(version=version)

    def test_invalid_run_refused(self):
        for run in ('../escape', '.', 'bad/run', ''):
            with self.subTest(run=run), self.assertRaisesRegex(ValueError, 'run'): self.build(run_id=run)

    def test_output_escape_refused(self):
        with self.assertRaisesRegex(ValueError, 'output'): self.build(output=Path(self.temp.name) / 'escape')
        self.assertFalse((Path(self.temp.name) / 'escape').exists())

    def test_existing_output_refused(self):
        target = self.root / 'tplab/first'; target.mkdir(parents=True)
        (target / 'keep').write_text('keep')
        with self.assertRaisesRegex(ValueError, 'exists'): self.build()
        self.assertEqual('keep', (target / 'keep').read_text())

    def test_git_symlink_refused(self):
        path = 'Assets/TPLab/Core/Pooling/ObjectPool.cs'
        blob = git(self.root, 'hash-object', path)
        git(self.root, 'update-index', '--cacheinfo', '120000', blob, path)
        git(self.root, 'commit', '-qm', 'symbolic input')
        self.revision = git(self.root, 'rev-parse', 'HEAD')
        with self.assertRaisesRegex(ValueError, 'symlink'): builder.read_inputs(self.root)

    def test_missing_license_refused(self):
        (self.root / 'LICENSE').unlink(); self.commit()
        with self.assertRaisesRegex(ValueError, 'LICENSE'): self.build()

    def test_document_escape_refused(self):
        with (self.root / 'doc/api/Pooling.md').open('a', encoding='utf-8') as f: f.write('\n[escape](../../../secret)\n')
        self.commit()
        with self.assertRaisesRegex(ValueError, 'escape'): self.build()

    def test_bad_local_link_refused(self):
        with (self.root / 'doc/api/Pooling.md').open('a', encoding='utf-8') as f: f.write('\n[missing](../../Assets/TPLab/Core/missing.cs)\n')
        self.commit()
        with self.assertRaisesRegex(ValueError, 'link'): self.build()

    def test_changed_guid_refused(self):
        meta = self.root / 'Assets/TPLab/Core/Pooling/ObjectPool.cs.meta'
        meta.write_text(meta.read_text().replace('guid:', 'bad_guid:'))
        self.commit()
        with self.assertRaisesRegex(ValueError, 'GUID'): self.build()


if __name__ == '__main__':
    unittest.main(verbosity=2)
