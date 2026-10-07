import importlib.util, json, tempfile, unittest, hashlib, io, tarfile
from pathlib import Path
spec=importlib.util.spec_from_file_location('consumer',Path(__file__).with_name('run_core_consumer.py'))
c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)
ROOT=Path(__file__).resolve().parents[1]
class DistributionConsumerTests(unittest.TestCase):
    def setUp(self):
        (ROOT/'tplab').mkdir(exist_ok=True)
        self.temp=tempfile.TemporaryDirectory(prefix='consumer-contract-',dir=ROOT/'tplab')
        self.addCleanup(self.temp.cleanup)
        self.artifacts=Path(self.temp.name)/'artifacts';self.artifacts.mkdir()
        self.revision='e'*40
        packages=[]
        for name in ('core','input','editor'):
            package_id='com.tplab.'+name;filename=package_id+'-0.0.1.tgz'
            data=json.dumps({'name':package_id,'version':'0.0.1'}).encode()
            with tarfile.open(self.artifacts/filename,'w:gz') as tar:
                member=tarfile.TarInfo('package/package.json');member.size=len(data);tar.addfile(member,io.BytesIO(data))
            payload={'package.json':hashlib.sha256(data).hexdigest()}
            packages.append({'id':package_id,'version':'0.0.1','filename':filename,'sha256':c.sha256(self.artifacts/filename),
                'payloadSha256':hashlib.sha256((json.dumps(payload,ensure_ascii=False,sort_keys=True,indent=2)+'\n').encode()).hexdigest()})
        self.manifest={'schemaVersion':1,'sourceRevision':self.revision,'publicationSnapshot':'Matched','version':'0.0.1','packages':packages}
        (self.artifacts/'distribution-manifest.json').write_text(json.dumps(self.manifest))

    def test_tarball_installs_artifacts_without_source_copy(self):
        plan=c.installation_plan(ROOT,'tarball',self.artifacts,self.revision,True,True,False)
        self.assertEqual('file:',plan['dependencies']['com.tplab.core'][:5])
        self.assertIn('com.tplab.editor',plan['dependencies'])
        self.assertNotIn('com.unity.ugui',plan['dependencies'])
    def test_git_uses_exact_sha_and_subfolder(self):
        revision=self.revision
        plan=c.installation_plan(ROOT,'git',self.artifacts,revision,False,False,False)
        self.assertEqual('https://github.com/KangSungKyu/TPLab.git?path=/upm/com.tplab.core#'+revision,plan['dependencies']['com.tplab.core'])
        self.assertNotIn('com.tplab.input',plan['dependencies'])
    def test_samples_require_input(self):
        with self.assertRaises(ValueError): c.installation_plan(ROOT,'tarball',self.artifacts,self.revision,False,False,True)
    def test_source_revision_mismatch_refused(self):
        with self.assertRaises(ValueError): c.installation_plan(ROOT,'git',self.artifacts,'0'*40,False,False,False)
    def test_wrong_installation_mode_refused(self):
        with self.assertRaises(ValueError): c.installation_plan(ROOT,'local',self.artifacts,self.revision,False,False,False)
    def test_source_copy_mode_keeps_its_original_manifest(self):
        with tempfile.TemporaryDirectory() as t:
            out=Path(t); copied=c.copy_allowlist(ROOT,out,ROOT/'tools',c.unity_version(ROOT))
            deps=json.loads((out/'Packages/manifest.json').read_text())['dependencies']
            self.assertNotIn('com.tplab.core',deps)
            self.assertTrue((out/'Assets/TPLab/Core/TPLab.Core.asmdef').is_file())
            self.assertTrue(copied)
    def test_artifact_mode_copies_harness_only(self):
        with tempfile.TemporaryDirectory() as t:
            out=Path(t); plan={'mode':'tarball','dependencies':{'com.tplab.core':'file:approved.tgz'},'includeEditor':False,'importSamples':False}
            copied=c.copy_allowlist(ROOT,out,ROOT/'tools',c.unity_version(ROOT),False,plan)
            self.assertEqual([],copied)
            self.assertFalse((out/'Assets/TPLab/Core').exists())
            self.assertTrue((out/'Assets/Editor/TPLabConsumerBuild.cs').is_file())
    def test_tampered_archive_refused_before_unity(self):
        with (self.artifacts/'com.tplab.core-0.0.1.tgz').open('ab') as archive: archive.write(b'tamper')
        with self.assertRaises(ValueError): c.installation_plan(ROOT,'tarball',self.artifacts,self.revision,False,False,False)
    def test_unreviewed_snapshot_refused(self):
        self.manifest['publicationSnapshot']='NotChecked'
        (self.artifacts/'distribution-manifest.json').write_text(json.dumps(self.manifest))
        with self.assertRaises(ValueError): c.installation_plan(ROOT,'git',self.artifacts,self.revision,False,False,False)
    def test_existing_output_refused(self):
        existing=ROOT/'Temp/DistributionConsumer-existing-contract'
        existing.mkdir(parents=True,exist_ok=False)
        try:
            with self.assertRaises(FileExistsError):c.resolve_paths(str(ROOT),str(existing),str(ROOT/'doc/validation/distribution-consumer'))
        finally:existing.rmdir()
    def test_changed_installed_dll_is_rejected(self):
        with tempfile.TemporaryDirectory() as t:
            output=Path(t);resolved=output/'Library/core';resolved.mkdir(parents=True)
            archive=self.artifacts/'com.tplab.core-0.0.1.tgz'
            binary=b'original-binary';member=tarfile.TarInfo('package/Runtime/CsvHelper.dll');member.size=len(binary)
            with tarfile.open(archive,'w:gz') as tar:tar.addfile(member,io.BytesIO(binary))
            (resolved/'Runtime').mkdir();(resolved/'Runtime/CsvHelper.dll').write_bytes(b'tampered-binary')
            provider='file:'+archive.as_posix()
            (output/'Packages').mkdir();(output/'Packages/packages-lock.json').write_text(json.dumps({'dependencies':{'com.tplab.core':{'version':provider,'source':'local-tarball'}}}))
            plan={'mode':'tarball','version':'0.0.1','dependencies':{'com.tplab.core':provider},'packages':[{'id':'com.tplab.core','filename':archive.name}],'artifacts':str(self.artifacts),'includeEditor':False,'importSamples':False}
            report={'installedPackages':[{'name':'com.tplab.core','version':'0.0.1','resolvedPath':str(resolved)}]}
            with self.assertRaises(RuntimeError):c.verify_installation(output,plan,report)

    def test_unity_fingerprint_is_only_allowed_manifest_change(self):
        expected=b'{"name":"com.tplab.core","version":"0.0.1"}'
        actual=b'{"name":"com.tplab.core","version":"0.0.1","_fingerprint":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}'
        self.assertTrue(c.installed_manifest_matches(expected,actual,'com.tplab.core@aaaaaaaaaaaa'))
        self.assertFalse(c.installed_manifest_matches(expected,actual.replace(b'0.0.1',b'9.0.0'),'com.tplab.core@aaaaaaaaaaaa'))
        self.assertFalse(c.installed_manifest_matches(expected,actual,'com.tplab.core@bbbbbbbbbbbb'))
        self.assertFalse(c.installed_manifest_matches(expected,b'{"name":"com.tplab.core","version":"0.0.1","extra":true}','com.tplab.core@aaaaaaaaaaaa'))

    def test_symlink_output_rejected(self):
        with tempfile.TemporaryDirectory() as t:
            outside=Path(t); linked=ROOT/'Temp/DistributionConsumer-test-link'
            linked.parent.mkdir(parents=True,exist_ok=True)
            try:
                linked.symlink_to(outside,target_is_directory=True)
            except OSError: self.skipTest('OS does not permit directory symlinks')
            try:
                with self.assertRaises(ValueError): c.resolve_paths(str(ROOT),str(linked/'escape'),str(ROOT/'doc/validation/distribution-consumer'))
            finally: linked.unlink()
if __name__=='__main__': unittest.main()
