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
                 'Assets/TPLab/UI/Runtime', 'Assets/TPLab/UI/InputSystem/Runtime', 'Assets/TPLab/Netcode/Runtime',
                 'Assets/TPLab/Samples/Core/CorePooling', 'Assets/TPLab/Samples/Input/SceneTransitions',
                 'Assets/TPLab/Samples/UI', 'Assets/TPLab/Samples/Netcode', 'Assets/Plugins/CsvHelper',
                 'doc/api', 'doc/ai/api', 'doc/licenses')
        exact = {'Assets/TPLab/Core.meta', 'Assets/TPLab/Input/Runtime.meta',
                 'Assets/TPLab/Editor.meta', 'Assets/TPLab/UI/Runtime.meta', 'Assets/TPLab/UI/InputSystem/Runtime.meta',
                 'Assets/TPLab/Netcode/Runtime.meta', 'Assets/Plugins/CsvHelper.meta',
                 'Assets/TPLab/Samples/Core/CorePooling.meta', 'Assets/TPLab/Samples/Input/SceneTransitions.meta',
                 'Assets/TPLab/Samples/UI.meta', 'Assets/TPLab/Samples/Netcode.meta',
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
        args = dict(source=self.root, revision=self.revision, version='0.0.2', run_id=run,
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

    def test_samples_are_optional_and_split_by_dependency(self):
        self.build()
        root = self.root / 'tplab/first/packages'
        core = json.loads((root / 'com.tplab.core/package.json').read_text())
        inp = json.loads((root / 'com.tplab.input/package.json').read_text())
        ui_input = json.loads((root / 'com.tplab.ui.inputsystem/package.json').read_text())
        netcode = json.loads((root / 'com.tplab.netcode/package.json').read_text())
        self.assertEqual('Samples~/CorePooling', core['samples'][0]['path'])
        self.assertEqual('Samples~/SceneTransitions', inp['samples'][0]['path'])
        self.assertNotIn('com.unity.inputsystem', core['dependencies'])
        self.assertNotIn('com.unity.ugui', core['dependencies'])
        self.assertTrue((root / 'com.tplab.input/Samples~/SceneTransitions/Settings/SampleInput.inputactions').is_file())
        self.assertFalse((root / 'com.tplab.editor/Samples~').exists())
        self.assertEqual('Samples~/UI', ui_input['samples'][0]['path'])
        self.assertEqual('Samples~/Netcode', netcode['samples'][0]['path'])
        self.assertIn('com.unity.ugui: 2.0.0', (root / 'com.tplab.input/README.md').read_text())
        self.assertIn('com.unity.inputsystem: 1.19.0', (root / 'com.tplab.netcode/README.md').read_text())
        self.assertIn('com.unity.ugui: 2.0.0', (root / 'com.tplab.netcode/README.md').read_text())

    def test_production_editor_validation_sources_are_preserved(self):
        self.build()
        source = self.root / 'Assets/TPLab/Editor/Bootstrap/BootstrapEditorValidation.cs'
        package = self.root / 'tplab/first/packages/com.tplab.editor/Editor/Bootstrap/BootstrapEditorValidation.cs'
        self.assertTrue(source.is_file())
        self.assertTrue(package.is_file(), 'production Editor Validation source was filtered from package')
        self.assertEqual(source.read_bytes(), package.read_bytes())
        self.assertEqual((self.root / (str(source.relative_to(self.root)) + '.meta')).read_bytes(),
                         (self.root / 'tplab/first/packages/com.tplab.editor/Editor/Bootstrap/BootstrapEditorValidation.cs.meta').read_bytes())

    def test_ui_and_netcode_packages_keep_optional_dependencies_separate(self):
        self.build()
        root = self.root / 'tplab/first/packages'
        expected = {
            'com.tplab.ui': {'com.tplab.core': '0.0.2', 'com.cysharp.unitask': '2.5.11', 'com.unity.ugui': '2.0.0'},
            'com.tplab.ui.inputsystem': {'com.tplab.ui': '0.0.2', 'com.tplab.input': '0.0.2',
                'com.cysharp.unitask': '2.5.11', 'com.unity.inputsystem': '1.19.0', 'com.unity.ugui': '2.0.0'},
            'com.tplab.netcode': {'com.unity.netcode.gameobjects': '2.13.3', 'com.cysharp.unitask': '2.5.11',
                'com.unity.collections': '2.6.6'},
        }
        for package_id, dependencies in expected.items():
            with self.subTest(package=package_id):
                manifest = json.loads((root / package_id / 'package.json').read_text())
                self.assertEqual('0.0.2', manifest['version'])
                self.assertEqual(dependencies, manifest['dependencies'])
                self.assertTrue((root / package_id / 'Documentation~/api/UI.md' if package_id.startswith('com.tplab.ui')
                                 else root / package_id / 'Documentation~/api/Netcode.md').is_file())
                self.assertTrue((root / package_id / 'Documentation~/ai/api/UI.md' if package_id.startswith('com.tplab.ui')
                                 else root / package_id / 'Documentation~/ai/api/Netcode.md').is_file())
        self.assertNotIn('com.tplab.core', expected['com.tplab.netcode'])
        self.assertNotIn('com.unity.inputsystem', json.loads((root / 'com.tplab.ui/package.json').read_text())['dependencies'])
        self.assertNotIn('com.unity.ugui', json.loads((root / 'com.tplab.core/package.json').read_text())['dependencies'])
        self.assertIn('Unity uGUI', (root / 'com.tplab.ui/Third Party Notices.md').read_text(encoding='utf-8'))
        netcode_notices = (root / 'com.tplab.netcode/Third Party Notices.md').read_text(encoding='utf-8')
        self.assertIn('Netcode for GameObjects', netcode_notices)
        self.assertIn('Unity Collections', netcode_notices)

    def test_new_samples_map_asmdefs_and_externalize_repository_links(self):
        self.build()
        root = self.root / 'tplab/first/packages'
        ui_adapter = root / 'com.tplab.ui.inputsystem'
        netcode = root / 'com.tplab.netcode'
        self.assertTrue((ui_adapter / 'Runtime/UIInputSystemAdapter.cs').is_file())
        self.assertTrue((ui_adapter / 'Samples~/UI/Editor/UIContextSampleBuilder.cs').is_file())
        ui_editor = json.loads((ui_adapter / 'Samples~/UI/Editor/TPLab.UISamples.OptionalInput.Editor.asmdef').read_text())
        self.assertIn('TPLab.UI.InputSystem', ui_editor['references'])
        self.assertIn('TPLab.Core.Input', ui_editor['references'])
        self.assertTrue((netcode / 'Samples~/Netcode/Editor/NetcodeSessionSampleBuilder.cs').is_file())
        netcode_editor = json.loads((netcode / 'Samples~/Netcode/Editor/TPLab.Netcode.Samples.Editor.asmdef').read_text())
        self.assertIn('Unity.Netcode.Editor', netcode_editor['references'])
        for package in (ui_adapter, netcode):
            readme = (package / 'Samples~' / ('UI' if package == ui_adapter else 'Netcode') / 'README.md').read_text(encoding='utf-8')
            self.assertNotIn('../../../../doc/', readme)
            self.assertIn('https://github.com/KangSungKyu/TPLab/blob/v0.0.2/doc/', readme)
            sample_tree = package / 'Samples~'
            self.assertFalse(any(part in ('Tests', 'Tests.meta') for path in sample_tree.rglob('*')
                                 for part in path.relative_to(sample_tree).parts))
        netcode_asmdef = json.loads((netcode / 'Runtime/TPLab.Netcode.asmdef').read_text())
        self.assertNotIn('TPLab.Core', netcode_asmdef['references'])
        self.assertTrue((netcode / 'Runtime/AssemblyInfo.cs').is_file())
        manifest = json.loads((self.root / 'tplab/first/artifacts/distribution-manifest.json').read_text())
        sample_sources = {'com.tplab.ui.inputsystem': 'Assets/TPLab/Samples/UI/README.md',
                          'com.tplab.netcode': 'Assets/TPLab/Samples/Netcode/README.md'}
        for package_id, source in sample_sources.items():
            record = next(item for item in manifest['sourceMapping']
                          if item['package'] == package_id and item['source'] == source)
            self.assertEqual(hashlib.sha256((self.root / source).read_bytes()).hexdigest(), record['sha256'])

    def test_all_six_packages_are_recorded_as_0_0_2(self):
        self.build()
        manifest = json.loads((self.root / 'tplab/first/artifacts/distribution-manifest.json').read_text())
        expected = {'com.tplab.core', 'com.tplab.input', 'com.tplab.editor', 'com.tplab.ui',
                    'com.tplab.ui.inputsystem', 'com.tplab.netcode'}
        self.assertEqual(expected, {item['id'] for item in manifest['packages']})
        self.assertTrue(all(item['version'] == '0.0.2' for item in manifest['packages']))
        sums = (self.root / 'tplab/first/artifacts/SHA256SUMS.txt').read_text()
        self.assertEqual(expected, {line.split('  ', 1)[1].removesuffix('-0.0.2.tgz') for line in sums.splitlines()})

    def test_packages_docs_licenses_payload(self):
        self.build()
        folder = self.root / 'tplab/first'
        manifest = json.loads((folder / 'artifacts/distribution-manifest.json').read_text())
        self.assertEqual(6, len(manifest['packages']))
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
            samples = tree / 'Samples~'
            if samples.exists():
                self.assertFalse(any(part in ('Tests', 'Tests.meta') for path in samples.rglob('*')
                                     for part in path.relative_to(samples).parts))
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
        install = (folder / 'artifacts/INSTALL.md').read_text(encoding='utf-8')
        self.assertIn('Version 0.0.2', install)
        self.assertIn('Netcode is standalone and does not require Core', install)
        self.assertNotIn('Supply explicit UniTask/Core', install)

    def test_generated_visible_package_files_have_stable_unique_meta(self):
        self.build('metas-a'); self.build('metas-b')
        guids = set()
        for package_id in builder.PACKAGES:
            first = self.root / 'tplab/metas-a/packages' / package_id
            second = self.root / 'tplab/metas-b/packages' / package_id
            for name in ('package.json', 'README.md', 'CHANGELOG.md', 'LICENSE.md', 'Third Party Notices.md'):
                meta = first / (name + '.meta')
                self.assertTrue(meta.is_file(), package_id + '/' + name)
                self.assertEqual(meta.read_bytes(), (second / (name + '.meta')).read_bytes())
                guid = next(line.removeprefix('guid: ') for line in meta.read_text().splitlines() if line.startswith('guid: '))
                self.assertNotIn(guid, guids)
                guids.add(guid)

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
