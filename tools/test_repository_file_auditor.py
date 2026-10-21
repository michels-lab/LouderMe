"""Synthetic local Git repository tests; no production file mutations."""
import datetime as dt
import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("file_steward",HERE/"repository_file_auditor.py")
audit=importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)

class FileStewardTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        subprocess.run(["git","init","-q",str(self.root)],check=True)
        for key,value in (("user.name","Test"),("user.email","test@example.invalid")):
            subprocess.run(["git","-C",str(self.root),"config",key,value],check=True)
        (self.root/"docs").mkdir()
        (self.root/"README.md").write_text("[ok](docs/notes.md) [bad](missing.md) [offsite](https://example.org)\n",encoding="utf-8")
        (self.root/"docs/notes.md").write_text("Last reviewed: 2026-01-01\n",encoding="utf-8")
        (self.root/"docs/copy.md").write_text("Last reviewed: 2026-01-01\n",encoding="utf-8")
        (self.root/"feature.py").write_text("print(1)\n")
        (self.root/"mystery.abc").write_text("x\n")
        subprocess.run(["git","-C",str(self.root),"add","-A"],check=True)
        subprocess.run(["git","-C",str(self.root),"commit","-qm","synthetic"],check=True)
        self.policy={**audit.DEFAULT,"canonical_logs":["PROJECT_LOG.md"]}
    def test_tracked_tree_and_findings(self):
        r=audit.scan(self.root,self.policy,dt.date(2026,10,21))
        self.assertEqual(r["tracked_files_total"],5)
        self.assertEqual(r["classified_files_total"],5)
        codes={f["code"] for f in r["findings"]}
        for code in ("BROKEN_RELATIVE_LINK","DUPLICATE_DOC_CONTENT","MISSING_LOG","OLD_REVIEW_DATE","UNKNOWN_PURPOSE"):
            self.assertIn(code,codes)
        self.assertFalse(r["deletion_approved"])
    def test_not_affected_by_untracked_temp(self):
        before=audit.scan(self.root,self.policy,dt.date(2026,10,21))
        (self.root/"unsaved_personal_notes.txt").write_text("private")
        after=audit.scan(self.root,self.policy,dt.date(2026,10,21))
        self.assertEqual(before["inventory_fingerprint"],after["inventory_fingerprint"])
        self.assertFalse(after["untracked_audited"])
    def test_link_boundaries(self):
        paths={"docs/a.md","docs/b.md"}
        self.assertEqual(audit.link_result("docs/a.md","b.md#fragment",paths),(True,"docs/b.md"))
        self.assertIsNone(audit.link_result("docs/a.md","https://example.com",paths))
        self.assertFalse(audit.link_result("docs/a.md","../../secret",paths)[0])
    def test_log_does_not_repeat_on_identical_snapshot(self):
        result=audit.scan(self.root,self.policy,dt.date(2026,10,21))
        audit.write_reports(self.root,result,self.policy)
        log=self.root/"docs/audits/FILE_GOVERNANCE_LOG.md"
        snapshot=log.read_text()
        audit.write_reports(self.root,result,self.policy)
        self.assertEqual(log.read_text(),snapshot)
        summary=(self.root/"reports/file-governance/SUMMARY.json").read_text()
        self.assertNotIn("unsaved_personal_notes",summary)
        self.assertNotIn("README.md",summary)
    def test_extensionless_openfoam_fields_and_timesteps(self):
        samples = {
            "cases_controlled_buoyancy/B14/0/metal/T": "simulation_input",
            "cases_buoyancy/B1/0/metal/T": "simulation_input",
            "cases_buoyancy/B1/10/metal/T": "simulation_output",
            "cleanproy/0/metal/T": "simulation_input",
            "cleanproy/system/controlDict": "simulation_input",
            "doctor_original/bcFourier/0/T": "simulation_input",
            "freesurface/FS-C01/constant/g": "simulation_input",
            "cases_controlled_buoyancy/B14/system/controlDict": "simulation_input",
            "cases_controlled_buoyancy/B14/10/metal/T": "simulation_output",
            "cases_controlled_buoyancy/B14/postProcessing/metal/T": "simulation_output",
            "cases_controlled_buoyancy/B14/logs/CourantMax_0": "simulation_log",
            "analysis_results/v15_v16_postpush_20261015/output.dat": "scientific_analysis",
        }
        for path, role in samples.items():
            with self.subTest(path=path):
                self.assertEqual(audit.identify(path,{})[0], role)
    def test_android_backup_rules_not_disposable(self):
        self.assertEqual(audit.identify("android/app/src/main/res/xml/backup_rules.xml", {})[0], "config")
    def test_historical_is_not_assumed_obsolete(self):
        self.assertIsNotNone(audit.HISTORY.search("docs/archive/2026-01-01-experiment.md"))
    def test_idempotent_digests(self):
        a=audit.scan(self.root,self.policy,dt.date(2026,10,21))
        b=audit.scan(self.root,self.policy,dt.date(2026,10,21))
        self.assertEqual(a["evidence_fingerprint"],b["evidence_fingerprint"])

if __name__=="__main__": unittest.main()
