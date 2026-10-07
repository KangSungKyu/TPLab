"""Deterministic UPM packaging from a clean exact Git checkout; no install/publish/Git writes."""
import argparse
import gzip
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import posixpath
import re
import subprocess
import sys
import tarfile
import zlib
from urllib.parse import quote, unquote

REPOSITORY = 'https://github.com/KangSungKyu/TPLab'
UNITASK = 'https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11'
CSV_HASH = '20101c398654a14bfd42bd78d7281f43197d19b7b3cf41c7aae93f1eaba65a61'
PACKAGES = {
    'com.tplab.core': {'title': 'TPLab Core',
        'roots': {'Assets/TPLab/Core': 'Runtime', 'Assets/Plugins/CsvHelper': 'Runtime/ThirdParty/CsvHelper'},
        'modules': ('Pooling', 'Lifecycle', 'Resources', 'DataTables', 'SceneManagement'),
        'dependencies': {'com.cysharp.unitask': '2.5.11', 'com.unity.addressables': '2.9.1'}},
    'com.tplab.input': {'title': 'TPLab Input', 'roots': {'Assets/TPLab/Input/Runtime': 'Runtime'},
        'modules': ('Input',), 'dependencies': {'com.tplab.core': None, 'com.cysharp.unitask': '2.5.11', 'com.unity.inputsystem': '1.19.0'}},
    'com.tplab.editor': {'title': 'TPLab Editor', 'roots': {'Assets/TPLab/Editor': 'Editor'},
        'modules': ('Editor',), 'dependencies': {'com.tplab.core': None, 'com.cysharp.unitask': '2.5.11', 'com.unity.addressables': '2.9.1', 'com.unity.nuget.newtonsoft-json': '3.2.2'}},
}
LINK = re.compile(r'(\[[^\]]+\]\()([^)]+)(\))')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def json_bytes(value):
    return (json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2) + '\n').encode('utf-8')


def git(root, *args):
    result = subprocess.run(['git', '-C', str(root), *args], capture_output=True)
    if result.returncode:
        raise ValueError('Git input validation failed: ' + args[0])
    return result.stdout


def no_links(path):
    for item in (path, *path.parents):
        if item.is_symlink() or (hasattr(item, 'is_junction') and item.is_junction()):
            raise ValueError('symlink/junction path refused')


def committed_bytes(root, entries, paths):
    ordered = sorted(paths)
    response = subprocess.run(['git', '-C', str(root), 'cat-file', '--batch'],
        input=('\n'.join(entries[p] for p in ordered) + '\n').encode('ascii'), capture_output=True, check=True).stdout
    stream = io.BytesIO(response); inputs = {}
    for path in ordered:
        header = stream.readline().decode('ascii').split()
        if len(header) != 3 or header[1] != 'blob': raise ValueError('Invalid committed blob: ' + path)
        inputs[path] = stream.read(int(header[2]))
        if stream.read(1) != b'\n': raise ValueError('Invalid committed blob boundary')
    return inputs


def read_inputs(root):
    entries = {}
    for record in git(root, 'ls-files', '--stage', '-z').split(b'\0'):
        if not record: continue
        header, name = record.split(b'\t', 1)
        mode, blob, stage = header.decode('ascii').split()
        if stage != '0' or mode not in ('100644', '100755'):
            raise ValueError('symlink or unresolved Git input refused')
        entries[name.decode('utf-8')] = blob
    required = {'LICENSE', 'THIRD_PARTY_NOTICES.md', 'doc/licenses/UniTask-LICENSE.txt', 'ProjectSettings/ProjectVersion.txt'}
    for config in PACKAGES.values():
        for prefix in config['roots']:
            required.add(prefix + '.meta')
            children = {p for p in entries if p.startswith(prefix + '/')}
            if not children: raise ValueError('Missing input root: ' + prefix)
            required.update(children)
        for module in config['modules']:
            required.update({'doc/api/' + module + '.md', 'doc/ai/api/' + module + '.md'})
    for path in required:
        if path not in entries: raise ValueError('Missing required input: ' + path)
        no_links(root / path)
        if not (root / path).is_file(): raise ValueError('Missing input file: ' + path)
    inputs = committed_bytes(root, entries, required)
    if b'm_EditorVersion: 6000.3.18f1' not in inputs['ProjectSettings/ProjectVersion.txt']:
        raise ValueError('Unsupported Unity version; validation required')
    if digest(inputs['Assets/Plugins/CsvHelper/CsvHelper.dll']) != CSV_HASH:
        raise ValueError('CsvHelper DLL hash differs from licensed 33.1.0 input')
    return inputs, entries


def convert_links(text, source_path, destination, mapping, version):
    def convert(match):
        target = match[2].strip('<>')
        if target.startswith(('#', 'https://', 'http://', 'mailto:')): return match[0]
        part, marker, anchor = target.partition('#')
        normalized = posixpath.normpath(posixpath.join(posixpath.dirname(source_path), unquote(part)))
        if normalized.startswith(('../', '/')) or normalized == '..' or ':' in normalized:
            raise ValueError('Document path escape refused: ' + source_path)
        mapped = mapping.get(normalized)
        if mapped is None:
            for prefix, out in sorted(mapping.items(), key=lambda pair: -len(pair[0])):
                if normalized.startswith(prefix + '/') and not PurePosixPath(prefix).suffix:
                    mapped = out + normalized[len(prefix):]; break
        if mapped is None:
            url = REPOSITORY + '/blob/v' + version + '/' + quote(normalized, safe='/')
        else:
            url = quote(posixpath.relpath(mapped, posixpath.dirname(destination) or '.'), safe='/')
        if marker: url += '#' + anchor
        return match[1] + url + match[3]
    if re.search(r'(?i)(?<![a-z])(?:[a-z]:[\\/]|file://|codex://)', text):
        raise ValueError('Internal absolute document reference refused: ' + source_path)
    return LINK.sub(convert, text)


def validate_tree(files):
    guids = set()
    for path, data in files.items():
        if path.startswith('/') or '..' in PurePosixPath(path).parts or '\\' in path:
            raise ValueError('Package path escape refused')
        if path.endswith('.meta'):
            match = re.search(rb'^guid: ([0-9a-f]{32})\r?$', data, re.M)
            if not match or match[1] in guids: raise ValueError('Missing or duplicate GUID: ' + path)
            guids.add(match[1])
        if path.endswith(('.cs', '.asmdef', '.dll')) and path + '.meta' not in files:
            raise ValueError('Missing asset meta: ' + path)
        if path.endswith('.md'):
            for match in LINK.finditer(data.decode('utf-8-sig')):
                target = match[2].strip('<>')
                if target.startswith(('#', 'https://', 'http://', 'mailto:')): continue
                local = posixpath.normpath(posixpath.join(posixpath.dirname(path), unquote(target.split('#')[0])))
                if local not in files and not any(p.startswith(local + '/') for p in files):
                    raise ValueError('Broken package link: ' + path + ' -> ' + local)
    return guids


def package_tree(package_id, config, inputs, version):
    mapping = {'LICENSE': 'LICENSE.md', 'THIRD_PARTY_NOTICES.md': 'Third Party Notices.md',
        'doc/api/README.md': 'Documentation~/api/README.md', 'doc/ai/api/README.md': 'Documentation~/ai/api/README.md',
        'doc/ai/README.md': 'Documentation~/ai/README.md', 'doc/licenses/UniTask-LICENSE.txt': 'ThirdPartyNotices~/UniTask-LICENSE.txt'}
    mapping.update(config['roots']); files = {}; provenance = []
    for prefix, target in config['roots'].items():
        for path in inputs:
            if path == prefix + '.meta' or path.startswith(prefix + '/'):
                out = target + path[len(prefix):]; files[out] = inputs[path]
                provenance.append({'source': path, 'target': out, 'sha256': digest(inputs[path])})
    if 'Runtime/ThirdParty/CsvHelper' in config['roots'].values():
        guid = digest(b'com.tplab.core/Runtime/ThirdParty')[:32]
        files['Runtime/ThirdParty.meta'] = ('fileFormatVersion: 2\nguid: ' + guid + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode('utf-8')
    for module in config['modules']:
        mapping['doc/api/' + module + '.md'] = 'Documentation~/api/' + module + '.md'
        mapping['doc/ai/api/' + module + '.md'] = 'Documentation~/ai/api/' + module + '.md'
    header = '# ' + config['title'] + '\n\nPackageVersion: ' + version + '. Package installation/Player validation: **NotRun**.\n\n'
    modules = '\n'.join('- [' + m + '](' + m + '.md)' for m in config['modules']) + '\n'
    files['Documentation~/api/README.md'] = (header + modules).encode('utf-8')
    files['Documentation~/ai/api/README.md'] = (header + modules).encode('utf-8')
    for source, target in mapping.items():
        if source.startswith(('doc/api/', 'doc/ai/api/')) and not source.endswith('/README.md'):
            text = convert_links(inputs[source].decode('utf-8-sig'), source, target, mapping, version)
            prefix = 'PackageVersion: ' + version + '. InstallationValidation: NotRun. Evidence below describes historical source checks, not this package installation.\n\n'
            files[target] = (prefix + text).encode('utf-8')
            provenance.append({'source': source, 'target': target, 'sha256': digest(inputs[source])})
    dependencies = {key: value or version for key, value in config['dependencies'].items()}
    files['package.json'] = json_bytes({'name': package_id, 'version': version, 'displayName': config['title'],
        'unity': '6000.3', 'unityRelease': '18f1', 'description': 'Shared Unity core: ' + ', '.join(config['modules']), 'license': 'MIT', 'dependencies': dependencies})
    for source in ('LICENSE', 'THIRD_PARTY_NOTICES.md', 'doc/licenses/UniTask-LICENSE.txt'):
        provenance.append({'source': source, 'target': mapping[source], 'sha256': digest(inputs[source])})
    files['LICENSE.md'] = inputs['LICENSE']
    files['ThirdPartyNotices~/UniTask-LICENSE.txt'] = inputs['doc/licenses/UniTask-LICENSE.txt']
    notices = convert_links(inputs['THIRD_PARTY_NOTICES.md'].decode('utf-8-sig'), 'THIRD_PARTY_NOTICES.md', 'Third Party Notices.md', mapping, version)
    files['Third Party Notices.md'] = ('Repository dependency inventory. CsvHelper DLL is included only in com.tplab.core.\n\n' + notices).encode('utf-8')
    install = ('## Install\n\nSupply dependencies in consumer Packages/manifest.json; Git dependencies cannot be declared in package.json.\n\n'
        '- UniTask 2.5.11: `' + UNITASK + '`\n- Package dependencies: `' + json.dumps(dependencies, sort_keys=True) + '`\n'
        '- Git URL after release: `' + REPOSITORY + '.git?path=/upm/' + package_id + '#v' + version + '`\n'
        '- Tarball: `' + package_id + '-' + version + '.tgz`; explicitly supply Core before Input/Editor.\n\n'
        'Tag and installation are not verified by this packager. Do not install alongside Assets source copies of the same assemblies/GUIDs.\n\n')
    files['README.md'] = (header + install + '[Human API](Documentation~/api/README.md) · [AI guide](Documentation~/ai/README.md) · [License](LICENSE.md) · [Third-party notices](Third%20Party%20Notices.md)\n\n'
        'Confirmed source environment: Unity 6000.3.18f1, Windows Mono. Other versions/platforms/IL2CPP are unverified.\n'
        'Samples and their UPM import integration are deferred to P2; this P1 package has no bundled samples.\n').encode('utf-8')
    files['Documentation~/ai/README.md'] = (header + '[Human README](../../README.md) · [AI API](api/README.md) · [Human API](../api/README.md)\n\n'
        'RequiredSequence: resolve explicit dependencies; choose owner/root; configure project DTO/action assets/callbacks; await preparation; use APIs; release leases/subscriptions; terminate the owner.\n\n'
        'Follow module ownership, threading, cancellation and failure contracts. Project UI/schema/save policy remains project-owned. Historical evidence is not installation validation.\n').encode('utf-8')
    files['CHANGELOG.md'] = ('# Changelog\n\n## ' + version + '\n\nP1 packaging candidate with human/AI API and original notices. Installation, samples and release validation are pending.\n').encode('utf-8')
    validate_tree(files)
    return files, provenance, dependencies


def archive_bytes(files):
    output = io.BytesIO()
    with gzip.GzipFile(fileobj=output, mode='wb', filename='', mtime=0, compresslevel=9) as compressed:
        with tarfile.open(fileobj=compressed, mode='w', format=tarfile.USTAR_FORMAT) as tar:
            for path, data in sorted(files.items()):
                item = tarfile.TarInfo('package/' + path)
                item.size = len(data); item.mode = 0o644; item.mtime = 0
                item.uid = item.gid = 0; item.uname = item.gname = ''
                tar.addfile(item, io.BytesIO(data))
    return output.getvalue()


def build(source, revision, version, run_id, output, prepare=False):
    if not re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', version): raise ValueError('Invalid version')
    if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_-]{0,63}', run_id): raise ValueError('Invalid run ID')
    if not re.fullmatch(r'[0-9a-f]{40}', revision): raise ValueError('Expected exact 40-character revision')
    root = Path(source).absolute(); no_links(root); root = root.resolve()
    target = Path(output).absolute(); no_links(target)
    if target != root / 'tplab' / run_id: raise ValueError('output must equal source/tplab/run-id')
    if target.exists(): raise ValueError('output already exists')
    actual_root = Path(git(root, 'rev-parse', '--show-toplevel').decode().strip()).resolve()
    if actual_root != root or git(root, 'rev-parse', 'HEAD').decode().strip() != revision: raise ValueError('Source root or revision mismatch')
    if git(root, 'status', '--porcelain', '--untracked-files=all'): raise ValueError('Source must be clean, including non-ignored untracked files')
    if not git(root, 'check-ignore', '--no-index', 'tplab/' + run_id).strip(): raise ValueError('output root must be ignored by Git')
    inputs, entries = read_inputs(root)
    trees = {}; packages = []; provenance = []; all_guids = set()
    for package_id, config in PACKAGES.items():
        files, mapping, dependencies = package_tree(package_id, config, inputs, version)
        guids = validate_tree(files)
        if all_guids.intersection(guids): raise ValueError('GUID duplicated across packages')
        all_guids.update(guids); trees[package_id] = files
        payload = json_bytes({p: digest(data) for p, data in sorted(files.items())})
        packages.append({'id': package_id, 'version': version, 'filename': package_id + '-' + version + '.tgz',
            'payloadSha256': digest(payload), 'dependencies': dependencies})
        provenance.extend(dict(package=package_id, **record) for record in mapping)
    if not prepare:
        expected = {'upm/' + package + '/' + path: data for package, files in trees.items() for path, data in files.items()}
        actual = {p for p in entries if p.startswith('upm/')}
        if actual != set(expected): raise ValueError('Publication snapshot file set differs; prepare and review it first')
        snapshot_bytes = committed_bytes(root, entries, actual)
        for path, data in expected.items():
            no_links(root / path)
            if snapshot_bytes[path] != data: raise ValueError('Publication snapshot content differs: ' + path)
    if git(root, 'status', '--porcelain', '--untracked-files=all'): raise ValueError('Source must remain clean')
    target.mkdir(parents=True); artifacts = target / 'artifacts'; artifacts.mkdir()
    for package in packages:
        files = trees[package['id']]
        for path, data in files.items():
            destination = target / 'packages' / package['id'] / path
            destination.parent.mkdir(parents=True, exist_ok=True); destination.write_bytes(data)
        archive = archive_bytes(files); (artifacts / package['filename']).write_bytes(archive)
        package['sha256'] = digest(archive)
    report = {'schemaVersion': 1, 'sourceRevision': revision, 'version': version, 'publishable': False,
        'publicationSnapshot': 'NotChecked' if prepare else 'Matched', 'packages': packages,
        'packagingToolchain': {'python': sys.version.split()[0], 'zlib': zlib.ZLIB_RUNTIME_VERSION},
        'sourceMappingSha256': digest(json_bytes(provenance)), 'sourceMapping': provenance,
        'sourceInputsSha256': {path: digest(data) for path, data in sorted(inputs.items())},
        'gates': {'packaging': 'Verified', 'installation': 'NotRun', 'player': 'NotRun', 'samples': 'NotRun', 'release': 'NotRun'}}
    (artifacts / 'distribution-manifest.json').write_bytes(json_bytes(report))
    (artifacts / 'SHA256SUMS.txt').write_text(''.join(p['sha256'] + '  ' + p['filename'] + '\n' for p in packages), encoding='utf-8', newline='\n')
    (artifacts / 'INSTALL.md').write_text('# Installation candidate\n\nVersion ' + version + '. Installation NotRun. Supply explicit UniTask/Core and selected Unity dependencies; follow each package README. Samples deferred to P2.\n', encoding='utf-8', newline='\n')
    (artifacts / 'VALIDATION.md').write_text('# Validation\n\nSourceRevision: ' + revision + '\n\nPackage links, GUIDs, DLL/license and archive format checked. UPM resolve, compile, Player, sample import and tag/Release validation: NotRun. publishable: false.\n', encoding='utf-8', newline='\n')
    if git(root, 'rev-parse', 'HEAD').decode().strip() != revision or git(root, 'status', '--porcelain', '--untracked-files=all'):
        raise ValueError('Source changed during build; discard this run')
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True, type=Path); parser.add_argument('--revision', required=True)
    parser.add_argument('--version', required=True); parser.add_argument('--run-id', required=True)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--prepare', action='store_true', help='Stage reviewed publication copies; not a release')
    try:
        report = build(**vars(parser.parse_args()))
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        parser.exit(1, str(error) + '\n')
    print('Created ' + str(len(report['packages'])) + ' packages; publishable=false; installation=NotRun')


if __name__ == '__main__':
    main()
