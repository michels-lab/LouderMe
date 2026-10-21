#!/usr/bin/env python3
"""Repo File Steward: full tracked-tree inventory and bounded document audit.

Classifies every tracked file; reads text docs up to policy limit. No deletions,
LLM executions, binary-content claims, solver runs, or cross-repository writes.
"""
from __future__ import annotations
import argparse
import datetime as dt
import fnmatch
import hashlib
import json
import re
import subprocess
from collections import Counter, defaultdict
from pathlib import Path, PurePosixPath
from urllib.parse import urlsplit, unquote

DEFAULT = {"schema_version": 1, "canonical_logs": [], "purpose_overrides": {},
           "ignore_paths": ["reports/file-governance/*", "docs/audits/FILE_GOVERNANCE_LOG.md"],
           "max_text_bytes": 262144, "stale_days": 120, "log_stale_days": 45,
           "markdown_finding_limit": 120}
LINK = re.compile(r'(?<!!)\[[^\]\n]*\]\((<?[^)\n]+>?)\)')
DATE = re.compile(r'(?im)\b(?:last reviewed|last updated|última revisión|última actualización|revisado el|actualizado el)\s*:?\s*\*{0,2}(20\d{2}-\d{2}-\d{2})\b')
OLD = re.compile(r'(?i)(?:^|[._/\-])(?:backup|bak|old|obsolete|deprecated|copy|copia|tmp|temp|draft)(?:[._/\-]|$)')
HISTORY = re.compile(r'(?i)(?:^|/)(?:archives?|historical|history|legacy|snapshots?|historial)(?:/|$)|(?:^|[/_-])20\d{2}[-_]\d{2}[-_]\d{2}')
SRC = set(".py .js .jsx .ts .tsx .c .h .cc .cpp .cs .kt .java .go .rs .sh .ps1 .cjs .mjs".split())
ASSET = set(".png .jpg .jpeg .webp .svg .gif .ico .woff .woff2 .ttf .mp3 .mp4 .ogg".split())
DATA = set(".csv .tsv .vtk .vtu .foam .stl .obj .step .iges .parquet".split())
BINARY = set(".exe .apk .msix .aab .zip .dll .so .dylib .jar .7z .pdf".split())

def git(root, *args, timeout=90):
    return subprocess.check_output(["git","-C",str(root),*args], timeout=timeout, stderr=subprocess.DEVNULL)

def inventory(root):
    items=[]
    for chunk in git(root, "ls-files", "--stage", "-z").split(b"\0"):
        if not chunk: continue
        spec, path = chunk.split(b"\t",1)
        mode, blob, stage = spec.decode().split()
        if stage == "0":
            items.append({"path":path.decode("utf-8","surrogateescape"),"mode":mode,"blob":blob})
    return sorted(items,key=lambda x:x["path"])

def identify(path, custom):
    if path in custom: return custom[path], "function declared by owner", "owner"
    p=path.lower(); name=PurePosixPath(path).name.lower(); ext=PurePosixPath(path).suffix.lower()
    if p.startswith(".github/workflows/"): return "workflow", "GitHub Actions automation", "rule"
    if p.startswith(".github/agents/") or "/agents/" in p or name=="agents.md": return "agent", "assistant role/instructions", "rule"
    if name in {"license","license.md","copying","notice"}: return "license","licensing/notice", "rule"
    if name.startswith("readme") or name in {"index.md","documentation_index.md"}: return "index","navigation/onboarding", "rule"
    if name.endswith("log.md") or name.endswith("bitacora.md") or "audit_log" in name or "bitacora" in p or "changelog" in name:
        return "bitacora","technical/audit change history", "rule"
    if ext in {".md",".rst",".adoc"}: return "document","manual/decision/report/historical note","inferred"
    if ext in SRC:
        return ("test" if re.search(r'(^|/)(tests?|__tests__)/|(^|/)test[_-]',p) else "source"),"code/test implementation","rule"
    if ext in ASSET: return "asset","visual/audio/font asset","rule"
    if ext in DATA: return "data","structured/research data (semantics not validated)","rule"
    if ext in BINARY: return "binary","binary/archive (not parsed)","rule"
    if ext in {".json",".yaml",".yml",".toml",".xml",".ini",".properties",".gradle"} or name in {"dockerfile","makefile",".gitignore",".gitattributes"}:
        return "config","machine-readable configuration","rule"
    return "unknown","function unknown: owner classification required","unknown"

def link_result(source, raw, paths):
    text=raw.strip().strip("<>").split(' "',1)[0].split(" '",1)[0]
    if not text or text.startswith(("#","{")): return None
    u=urlsplit(text)
    if u.scheme or u.netloc or text.startswith("//"): return None
    rel=unquote(u.path)
    if not rel or rel.startswith("/") or "\\" in rel: return None
    parts=list(PurePosixPath(source).parent.parts)
    for part in rel.split("/"):
        if part in ("","."): continue
        if part=="..":
            if not parts: return False,"outside-repo"
            parts.pop()
        else: parts.append(part)
    dest="/".join(parts)
    return (dest in paths or dest.rstrip("/")+"/index.md" in paths or any(x.startswith(dest.rstrip("/")+"/") for x in paths), dest)

def log_age(root, path, today):
    try:
        d=git(root,"log","-1","--format=%cs","--",path,timeout=15).decode().strip()
        return (today-dt.date.fromisoformat(d)).days if d else None
    except (ValueError,subprocess.CalledProcessError,subprocess.TimeoutExpired): return None

def scan(root, policy=None, today=None):
    root=Path(root).resolve()
    policy={**DEFAULT, **(policy or {})}
    if policy.get("schema_version")!=1 or not isinstance(policy.get("purpose_overrides"),dict):
        raise ValueError("Invalid audit policy")
    today=today or dt.datetime.now(dt.timezone.utc).date()
    files=inventory(root)
    paths={x["path"] for x in files}
    listed=[]; flags=[]; duplicates=defaultdict(list); inspected=large=excluded=0
    def add(code, severity, path, observation, recommendation):
        fid=hashlib.sha256((code+"\0"+path+"\0"+observation).encode()).hexdigest()[:16]
        flags.append({"id":fid,"code":code,"severity":severity,"path":path,
                      "observation":observation,"recommendation":recommendation,
                      "state":"REVIEW_REQUIRED"})
    for file in files:
        path=file["path"]
        if any(fnmatch.fnmatchcase(path,p) for p in policy["ignore_paths"]):
            excluded+=1; continue
        role,purpose,certainty=identify(path,policy["purpose_overrides"])
        listed.append({"path":path,"role":role,"purpose":purpose,"certainty":certainty,"blob":file["blob"]})
        if role in {"document","index","bitacora"}: duplicates[file["blob"]].append(path)
        if role=="unknown":
            add("UNKNOWN_PURPOSE","review",path,"No reliable purpose category from path","Declare role after reviewing usage")
        if OLD.search(path) and not HISTORY.search(path):
            add("POSSIBLY_REDUNDANT_NAME","review",path,"Backup/old/copy/draft name; may be intentional","Check references and history before archiving")
        if role not in {"document","index","bitacora"}: continue
        target=(root/path).resolve()
        if not target.is_relative_to(root) or not target.is_file():
            add("UNAVAILABLE_TEXT","warning",path,"Tracked file unavailable/unsafe symlink","Inspect checkout/LFS/symlink")
            continue
        try:
            if target.stat().st_size>int(policy["max_text_bytes"]):
                large+=1; continue
            content=target.read_text(encoding="utf-8")
        except (OSError,UnicodeError):
            add("UNREADABLE_DOC","warning",path,"Not readable as UTF-8","Check encoding and source")
            continue
        inspected+=1
        for m in LINK.finditer(content):
            check=link_result(path,m.group(1),paths)
            if check and not check[0]:
                add("BROKEN_RELATIVE_LINK","confirmed",path,"Missing: "+check[1][:160],"Fix link or restore target")
        d=DATE.search(content[:14000])
        if d and not HISTORY.search(path):
            try:
                age=(today-dt.date.fromisoformat(d.group(1))).days
                if age>int(policy["stale_days"]):
                    add("OLD_REVIEW_DATE","review",path,
                        "Last stated review "+d.group(1)+" ("+str(age)+" days)",
                        "Recheck against code/requirements before changing")
            except ValueError: pass
    for _,group in duplicates.items():
        if len(group)>1:
            add("DUPLICATE_DOC_CONTENT","review",group[0],
                "Identical tracked text: "+", ".join(group[:8]),
                "Confirm aliases or translated/historical copies before consolidation")
    for path in policy["canonical_logs"]:
        if path not in paths:
            add("MISSING_LOG","confirmed",path,"Declared canonical bitacora absent","Restore authoritative log")
        else:
            days=log_age(root,path,today)
            if days is not None and days>int(policy["log_stale_days"]):
                add("LOG_UPDATE_REVIEW","review",path,
                    "Last committed "+str(days)+" days ago; not proof of omission",
                    "Compare product changes with this log")
    flags.sort(key=lambda f:({"confirmed":0,"warning":1,"review":2}[f["severity"]],f["code"],f["path"]))
    digest=lambda obj: hashlib.sha256(json.dumps(obj,separators=(",",":"),ensure_ascii=False).encode()).hexdigest()
    return {"schema_version":1,"audit_kind":"deterministic_full_tracked_tree",
            "tracked_files_total":len(files),"classified_files_total":len(listed),
            "docs_content_inspected":inspected,"large_docs_skipped":large,"excluded_reports":excluded,
            "roles":dict(sorted(Counter(x["role"] for x in listed).items())),
            "finding_counts":dict(sorted(Counter(x["code"] for x in flags).items())),
            "finding_total":len(flags),"inventory_fingerprint":digest([(x["path"],x["blob"]) for x in listed]),
            "evidence_fingerprint":digest([(x["id"],x["severity"]) for x in flags]),
            "files":listed,"findings":flags,"untracked_audited":False,"ignored_audited":False,
            "binary_contents_audited":False,"scientific_validation":"NOT_CHECKED",
            "app_installed_acceptance":"NOT_CHECKED","deletion_approved":False}

def write_reports(root, report, policy):
    out=root/"reports/file-governance"
    out.mkdir(parents=True,exist_ok=True)
    detail=out/"FILE_GOVERNANCE.json"; prior={}
    if detail.exists():
        try: prior=json.loads(detail.read_text(encoding="utf-8"))
        except (ValueError,OSError): pass
    detail.write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    summary={k:v for k,v in report.items() if k not in {"files","findings"}}
    (out/"SUMMARY.json").write_text(json.dumps(summary,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    lines=["# File steward: repository file audit","","All tracked files classified; Markdown/text docs inspected with size cap. Binary/research-content validity and untracked files not assessed.",
           "No file is auto-deleted or declared unused.","",
           "## Coverage","",
           "Tracked: "+str(report["tracked_files_total"])+"; classified: "+str(report["classified_files_total"])+
           "; readable documents: "+str(report["docs_content_inspected"])+
           "; above read cap: "+str(report["large_docs_skipped"])+
           "; findings: "+str(report["finding_total"])+".","",
           "## File roles","","| Role | Count |","|---|---:|"]
    lines += ["| "+k+" | "+str(v)+" |" for k,v in report["roles"].items()]
    lines += ["","## Findings (human review required)",""]
    for f in report["findings"][:int(policy["markdown_finding_limit"])]:
        lines.append("- **"+f["severity"].upper()+" / "+f["code"]+"** "+f["path"]+
            ": "+f["observation"]+". Next: "+f["recommendation"]+" (ID "+f["id"]+")")
    if report["finding_total"]>int(policy["markdown_finding_limit"]):
        lines.append("Remaining "+str(report["finding_total"]-int(policy["markdown_finding_limit"]))+" in complete JSON.")
    lines += ["","Complete path → purpose/role/certainty list lives in FILE_GOVERNANCE.json. SUMMARY.json has counts only for master polling.",
              "A dated file, duplicate, or suspicious name is an **investigation candidate**, not unwanted material.",
              "No historical science cases, audit logs or evidence may be deleted automatically.",""]
    (out/"FILE_GOVERNANCE.md").write_text("\n".join(lines),encoding="utf-8")
    if prior.get("inventory_fingerprint")!=report["inventory_fingerprint"] or prior.get("evidence_fingerprint")!=report["evidence_fingerprint"]:
        log=root/"docs/audits/FILE_GOVERNANCE_LOG.md"
        log.parent.mkdir(parents=True,exist_ok=True)
        if not log.exists():
            log.write_text("# Local file audit bitacora\n\nAutomatic evidence deltas, pending human triage. No deletion authority.\n",encoding="utf-8")
        with log.open("a",encoding="utf-8") as f:
            f.write("\n## "+dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%d %H:%M UTC")+"\n\n")
            f.write("- All tracked "+str(report["tracked_files_total"])+
                "; findings "+str(report["finding_total"])+
                "; inventory "+report["inventory_fingerprint"][:12]+
                "; evidence "+report["evidence_fingerprint"][:12]+
                ". Full local report in reports/file-governance/. Status REVIEW_REQUIRED.\n")

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--no-write",action="store_true")
    args=parser.parse_args()
    root=Path(args.repo).resolve(); policy=dict(DEFAULT)
    config=root/".michelslab/file-audit-policy.json"
    if config.exists(): policy.update(json.loads(config.read_text(encoding="utf-8")))
    report=scan(root,policy)
    if not args.no_write: write_reports(root,report,policy)
    print(json.dumps({k:report[k] for k in ("tracked_files_total","classified_files_total","docs_content_inspected","finding_total","finding_counts")},indent=2))
if __name__=="__main__": main()
