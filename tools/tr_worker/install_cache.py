"""ROB RUN TR Addressables-cache installer (+ Turkish TMP font glyphs)."""
from __future__ import annotations

import argparse
import json
import marshal
import os
import re
import shutil
import sys
import types
from pathlib import Path

os.environ.setdefault("PYTHONUTF8", "1")
os.environ.setdefault("PYTHONIOENCODING", "utf-8")
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # type: ignore[attr-defined]
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")  # type: ignore[attr-defined]
except Exception:
    pass

import UnityPy

CACHE = Path(
    os.path.expandvars(
        r"%USERPROFILE%\AppData\LocalLow\Kefir\ROB RUN\com.unity.addressables"
    )
)
MARKER = CACHE / ".robrun-tr-cache-patch"
FONT_CACHE = Path(
    os.path.expandvars(r"%LOCALAPPDATA%\RobRunTurkceYama\inbuild_assets_all_turkish_.bundle")
)

EN_BUNDLE = (
    "localization-string-tables-english(unitedstates)(en-us)_assets_all.bundle"
)
TR_BUNDLE = (
    "localization-string-tables-turkish(turkey-republic)(tr)_assets_all_turkish_.bundle"
)
TR_PLAIN_BUNDLE = (
    "localization-string-tables-turkish(turkey-republic)(tr)_assets_all_turkish_plain_.bundle"
)
INB_BUNDLE = "inbuild_assets_all.bundle"
INB_TR_BUNDLE = "inbuild_assets_all_turkish_.bundle"

RT = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}\\StandaloneWindows64\\"
PD = "{UnityEngine.Application.persistentDataPath}/com.unity.addressables/"
REDIRECTS = [
    (RT + EN_BUNDLE, PD + TR_BUNDLE),
    (RT + INB_BUNDLE, PD + INB_TR_BUNDLE),
]

TABLE_PREFIXES = ("StaticStrings", "RealmNames", "TimeFormatterAbbreviations")
# Builtin / loading fontlari: noktali Turkce glifleri Latin'e katla (bos kutu olmasin)
PLAIN_LETTERS = str.maketrans(
    "ıİşŞğĞçÇöÖüÜâÂîÎûÛêÊôÔ",
    "iIsSgGcCoOuUaAiIuUeEoO",
)

# C# tr-TR ToUpper: i -> İ (fontta yok = bos). Bu anahtarlar UI'da buyuk harfe cevriliyor.
UPPERCASE_UI_KEYS = frozenset(
    {
        "early_access",
        "attention_screen_title",
        "restart_server_title",
        "reconnect_title",
        "quit_header",
        "error_title",
        "ban_modal_dialog_title",
        "playtest_waiting_for_start_title",
        "missing_localization",
    }
)

# Bu satir sinirli font + tr ToUpper kullaniyor; birlesik isaretler bosluklu bozuluyor.
# Temiz ASCII buyuk harf (I/S) okunur; gercek İ/Ş o fontta yok.
EARLY_ACCESS_ASCII = (
    "ERKEN ERISIM. OYUNUN NIHAI (SON) KALITESINI YANSITMAZ. / "
    "CEVIRI: ICESTAR - ROBRUNTURKCEYAMA.COM"
)


def fold_builtin_text(text: str, key: str | None = None) -> str:
    """Builtin font: bos glif + Turkce I sorununu cozer."""
    if key == "early_access":
        return EARLY_ACCESS_ASCII
    text = text.translate(PLAIN_LETTERS)
    if key and key in UPPERCASE_UI_KEYS:
        text = "".join((c.upper() if "a" <= c <= "z" else c) for c in text)
    return text
DICT_NAMES = ("robrun_tr.json", "master_tr.json")


def log(msg: str) -> None:
    print(msg, flush=True)


def app_dir() -> Path:
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        return Path(sys._MEIPASS)
    return Path(__file__).resolve().parent


def find_data_dir() -> Path:
    base = app_dir()
    for p in (base / "data", base):
        if any((p / n).exists() for n in DICT_NAMES) or (p / TR_BUNDLE).exists():
            return p
    raise SystemExit("robrun_tr.json / TR paketi bulunamadi.")


def load_dict(data: Path) -> dict:
    for name in DICT_NAMES:
        path = data / name
        if path.exists():
            return json.loads(path.read_text(encoding="utf-8"))
    raise SystemExit("robrun_tr.json bulunamadi.")


def load_font_tr():
    path = app_dir() / "font_tr.bin"
    if not path.exists():
        raise FileNotFoundError(f"font_tr.bin yok: {path}")
    mod = types.ModuleType("font_tr")
    exec(marshal.loads(path.read_bytes()), mod.__dict__)
    return mod


def find_game_dir() -> Path:
    roots: list[Path] = []
    try:
        import winreg

        for hive, sub, val in (
            (winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam", "SteamPath"),
            (
                winreg.HKEY_LOCAL_MACHINE,
                r"SOFTWARE\WOW6432Node\Valve\Steam",
                "InstallPath",
            ),
        ):
            try:
                with winreg.OpenKey(hive, sub) as k:
                    roots.append(Path(winreg.QueryValueEx(k, val)[0]))
            except OSError:
                pass
    except ImportError:
        pass

    roots.extend(
        [
            Path(r"D:\Steam"),
            Path(r"D:\SteamLibrary"),
            Path(r"C:\Program Files (x86)\Steam"),
        ]
    )
    libs: list[Path] = []
    for steam in roots:
        if not steam.exists():
            continue
        libs.append(steam)
        vdf = steam / "steamapps" / "libraryfolders.vdf"
        if vdf.exists():
            text = vdf.read_text(encoding="utf-8", errors="ignore")
            for m in re.findall(r'"path"\s+"([^"]+)"', text):
                libs.append(Path(m.replace("\\\\", "\\")))

    for lib in dict.fromkeys(libs):
        for name in ("ROB RUN", "Rob Run"):
            cand = lib / "steamapps" / "common" / name
            if (cand / "ROB RUN.exe").exists():
                return cand
    raise SystemExit("ROB RUN klasoru bulunamadi. Gozat ile sec.")


def uses_builtin_font(name: str) -> bool:
    return name.startswith("loading_tip") or name.startswith("...")


def catalog_names(aa: Path) -> tuple[str, str]:
    settings_path = aa / "settings.json"
    if settings_path.exists():
        settings = json.loads(settings_path.read_text(encoding="utf-8"))
        for loc in settings.get("m_CatalogLocations", []):
            if "AddressablesMainContentCatalogCacheHash" not in str(loc):
                continue
            internal = loc.get("m_InternalId") or ""
            base = os.path.basename(internal.replace("\\", "/"))
            if not base:
                continue
            if base.endswith(".hash"):
                return base[:-5] + ".bin", base
            if base.endswith(".bin"):
                return base, base[:-4] + ".hash"
            return base + ".bin", base + ".hash"

    sa = aa / "StandaloneWindows64"
    bins = sorted(sa.glob("catalog_*.bin"))
    if not bins:
        raise SystemExit("settings.json / catalog_*.bin yok.")
    return bins[0].name, bins[0].name[:-4] + ".hash"


def find_bundle(*names: str) -> Path | None:
    exe_dir = Path(sys.executable).resolve().parent
    roots = [
        app_dir() / "data",
        app_dir(),
        Path(__file__).resolve().parent / "data",
        Path(__file__).resolve().parent,
        exe_dir,
        exe_dir / "data",
        exe_dir / "PatchData",
        FONT_CACHE.parent,
    ]
    for root in roots:
        for name in names:
            p = root / name
            if p.exists() and p.stat().st_size > 1000:
                return p
    if FONT_CACHE.exists() and FONT_CACHE.stat().st_size > 1000:
        return FONT_CACHE
    return None


def install_strings(fonts_ok: bool, sa: Path, data: Path) -> None:
    """Font varsa tam Turkce (Ş/İ); yoksa Latin katlamali paket (bos glif olmasin)."""
    CACHE.mkdir(parents=True, exist_ok=True)
    if fonts_ok:
        src = find_bundle(TR_BUNDLE)
        if src:
            shutil.copy2(src, CACHE / TR_BUNDLE)
            log("metin: tam Turkce (fontlu)")
            return
    else:
        src = find_bundle(TR_PLAIN_BUNDLE, TR_BUNDLE)
        if src and src.name.endswith("_plain_.bundle"):
            shutil.copy2(src, CACHE / TR_BUNDLE)
            log("metin: Latin harfli TR (font yok — Ş/İ bos dusmesin)")
            return
        # tam TR varsa kurulumda katla
        full = find_bundle(TR_BUNDLE)
        if full:
            fold_tr_bundle(full, CACHE / TR_BUNDLE)
            log("metin: TR katlandi (font yok)")
            return

    # son care: EN'den uret
    build_translated_strings(sa, data, fold=not fonts_ok)


def fold_tr_bundle(src: Path, dest: Path) -> None:
    env = UnityPy.load(str(src))
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        tree = obj.read_typetree()
        changed = False
        for entry in tree.get("m_TableData", []) or []:
            t = entry.get("m_Localized") or ""
            n = t.translate(PLAIN_LETTERS)
            if n != t:
                entry["m_Localized"] = n
                changed = True
        if changed:
            obj.save_typetree(tree)
    dest.write_bytes(env.file.save(packer="lz4"))


def build_translated_strings(sa: Path, data: Path, fold: bool) -> None:
    tr_dict = load_dict(data)
    shared = json.loads((data / "shared_keys.json").read_text(encoding="utf-8"))
    builtin = set(json.loads((data / "builtin_keys.json").read_text(encoding="utf-8")))
    extra_path = data / "extra_tr.json"
    if not extra_path.exists():
        extra_path = app_dir() / "extra_tr.json"
    extra = json.loads(extra_path.read_text(encoding="utf-8")) if extra_path.exists() else {}

    en_path = sa / EN_BUNDLE
    if not en_path.exists():
        raise SystemExit(f"Ingilizce dil paketi yok: {en_path}")

    try:
        env = UnityPy.load(str(en_path))
    except Exception as ex:
        raise SystemExit(
            f"Dil paketi okunamadi ({type(ex).__name__}). Steam dosya dogrula."
        ) from ex

    done = total = 0
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        tree = obj.read_typetree()
        name = tree.get("m_Name") or ""
        prefix = next((p for p in TABLE_PREFIXES if name.startswith(p)), None)
        if not prefix:
            continue
        id_to_key = shared.get(prefix, {})
        for entry in tree.get("m_TableData", []):
            total += 1
            eid = str(entry.get("m_Id"))
            key = id_to_key.get(eid)
            en = entry.get("m_Localized") or ""
            lkey = f"{prefix}::{key}" if key else None
            text = tr_dict.get(lkey) if lkey else None
            if text is None and en in extra:
                text = extra[en]
            if text is None:
                continue
            # builtin_keys = sinirli font (early_access, loading_tip, start_game_...)
            if fold or (key in builtin) or (eid in builtin) or uses_builtin_font(key or ""):
                text = fold_builtin_text(text, key)
            if entry.get("m_Localized") != text:
                entry["m_Localized"] = text
                done += 1
        obj.save_typetree(tree)

    CACHE.mkdir(parents=True, exist_ok=True)
    (CACHE / TR_BUNDLE).write_bytes(env.file.save(packer="lz4"))
    log(f"metin: {done}/{total} satir Turkce" + (" (katlamali)" if fold else ""))


_LAST_FONT_ERROR = ""


def set_font_error(msg: str) -> None:
    global _LAST_FONT_ERROR
    _LAST_FONT_ERROR = msg
    log(f"font: {msg}")


def install_prebuilt_fonts() -> bool:
    src = find_bundle(INB_TR_BUNDLE)
    if src is None:
        return False
    CACHE.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, CACHE / INB_TR_BUNDLE)
    log(f"font: hazir Turkce font paketi ({src.name})")
    return True


def resolve_inbuild_source(sa: Path) -> Path | None:
    """XYRO gibi once duz ad; yoksa hash'li Addressables adi; sonra oyun agaci / cache."""
    direct = sa / INB_BUNDLE
    if direct.exists() and direct.stat().st_size > 1000:
        return direct

    hashed = [
        p
        for p in sa.glob("inbuild_assets_all_*.bundle")
        if "turkish" not in p.name.lower() and p.stat().st_size > 1000
    ]
    if hashed:
        hashed.sort(key=lambda p: p.stat().st_size, reverse=True)
        return hashed[0]

    # LocalLow'da onceki indirme (hash'li)
    if CACHE.is_dir():
        for p in CACHE.glob("inbuild_assets_all*.bundle"):
            if "turkish" in p.name.lower():
                continue
            if p.stat().st_size > 1000:
                return p

    # oyun klasoru altinda ara (StreamingAssets disi kopyalar)
    try:
        game = sa.parents[3]  # .../ROB RUN
    except IndexError:
        game = sa
    if game.exists():
        for p in game.rglob("inbuild_assets_all*.bundle"):
            if "turkish" in p.name.lower():
                continue
            if p.stat().st_size > 1000:
                return p
    return None


def build_turkish_fonts(sa: Path) -> bool:
    """Oyundaki inbuild TMP atlasina ıİşŞğĞ gliflerini cizer (site fontu degil — oyun fontu)."""
    src = resolve_inbuild_source(sa)
    if src is None:
        set_font_error(
            f"inbuild_assets_all.bundle yok ({sa}). "
            "Steam → ROB RUN → Dosyalari dogrula, oyunu bir kez acip kapat, tekrar dene."
        )
        return False

    try:
        font_tr = load_font_tr()
        log(f"font: TMP atlas yamalaniyor ({src.name}, {src.stat().st_size} bayt)...")
        env = UnityPy.load(str(src))
        added = font_tr.patch_font_bundle(env)
        CACHE.mkdir(parents=True, exist_ok=True)
        out = CACHE / INB_TR_BUNDLE
        out.write_bytes(env.file.save(packer="lz4"))
        try:
            FONT_CACHE.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(out, FONT_CACHE)
        except OSError:
            pass
        log(f"font: {added} Turkce harf eklendi")
        return True
    except BaseException as ex:
        set_font_error(f"basarisiz ({type(ex).__name__}: {ex})")
        bad = CACHE / INB_TR_BUNDLE
        if bad.exists():
            try:
                bad.unlink()
            except OSError:
                pass
        return False


def try_fonts_in_subprocess(game: Path) -> bool:
    """Font native crash ana kurulumu dusurmesin diye ayri process."""
    if install_prebuilt_fonts():
        return True

    exe = Path(sys.executable)
    # frozen worker: sys.executable = tr_cache_worker.exe
    cmd = [str(exe), "--font-only", "--game", str(game)]
    if not getattr(sys, "frozen", False):
        cmd = [str(exe), str(Path(__file__).resolve()), "--font-only", "--game", str(game)]

    log("font: ayri surecte deneniyor...")
    import subprocess

    try:
        r = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=180,
        )
        if r.stdout:
            for line in r.stdout.strip().splitlines()[-5:]:
                log(line)
        if r.returncode == 0 and (CACHE / INB_TR_BUNDLE).exists():
            return True
        if r.stderr:
            err = r.stderr.strip().splitlines()[-1] if r.stderr.strip() else ""
            if err:
                log(f"font: alt surec: {err[:160]}")
        log(f"font: atlandi (kod {r.returncode})")
    except subprocess.TimeoutExpired:
        log("font: zaman asim — atlandi")
    except BaseException as ex:
        log(f"font: atlandi ({type(ex).__name__})")
    return False


def font_only(game: Path) -> int:
    sa = game / "ROB RUN_Data" / "StreamingAssets" / "aa" / "StandaloneWindows64"
    if not sa.exists():
        print(f"StandaloneWindows64 yok: {sa}", file=sys.stderr)
        return 2
    ok = build_turkish_fonts(sa)
    return 0 if ok else 3


def write_catalog(sa: Path, aa: Path, fonts_ok: bool) -> None:
    bin_name, hash_name = catalog_names(aa)
    src_bin = sa / bin_name
    if not src_bin.exists():
        bins = sorted(sa.glob("catalog_*.bin"))
        if not bins:
            raise SystemExit(f"Katalog yok: {bin_name}")
        src_bin = bins[0]
        bin_name = src_bin.name
        hash_name = bin_name[:-4] + ".hash"

    log(f"katalog: {bin_name}")
    raw = src_bin.read_bytes()
    patched = raw
    replaced = 0
    for old, new in REDIRECTS:
        old_b, new_b = old.encode("ascii"), new.encode("ascii")
        if len(old_b) != len(new_b):
            raise SystemExit(
                f"Yonlendirme uzunluklari esit olmali ({len(old_b)} != {len(new_b)})."
            )
        if raw.count(old_b) < 1:
            if "inbuild" in old:
                continue
            raise SystemExit(
                "Katalogda beklenen adres yok — oyun formati degismis olabilir."
            )
        if "inbuild" in old and not fonts_ok:
            continue
        patched = patched.replace(old_b, new_b)
        replaced += 1

    if replaced < 1:
        raise SystemExit("Kataloga hic yonlendirme yazilamadi.")

    CACHE.mkdir(parents=True, exist_ok=True)
    for fn in list(CACHE.iterdir()):
        if fn.name.startswith("catalog_"):
            fn.unlink()

    (CACHE / bin_name).write_bytes(patched)
    src_hash = sa / hash_name
    if src_hash.exists():
        shutil.copy2(src_hash, CACHE / hash_name)
    log(f"katalog: {replaced} yonlendirme yazildi")


def install(game: Path | None = None) -> None:
    for old, new in REDIRECTS:
        if len(old) != len(new):
            raise SystemExit(
                f"Yonlendirme uzunluklari esit olmali ({len(old)} != {len(new)})."
            )

    game = Path(game) if game else find_game_dir()
    aa = game / "ROB RUN_Data" / "StreamingAssets" / "aa"
    sa = aa / "StandaloneWindows64"
    if not sa.exists():
        raise SystemExit(f"StandaloneWindows64 yok: {sa}")

    log(f"oyun: {game}")
    data = find_data_dir()

    CACHE.mkdir(parents=True, exist_ok=True)
    for fn in list(CACHE.iterdir()):
        if fn.name.startswith("catalog_"):
            fn.unlink()

    # RobRun_TR ile ayni sira: once font (Python 3.12 worker), sonra metin, sonra katalog
    fonts_ok = install_prebuilt_fonts()
    if not fonts_ok:
        # once in-process (3.12'de calisir); olmezse ayri surec
        fonts_ok = build_turkish_fonts(sa)
    if not fonts_ok:
        fonts_ok = try_fonts_in_subprocess(game)

    if not fonts_ok:
        detail = _LAST_FONT_ERROR or "bilinmeyen font hatasi"
        raise SystemExit(
            "Turkce font (inbuild) yazilamadi — menude Ş/İ bos kalir. " + detail
        )

    install_strings(fonts_ok=True, sa=sa, data=data)
    if not (CACHE / TR_BUNDLE).exists():
        raise SystemExit("Turkce dil paketi olusturulamadi.")

    write_catalog(sa, aa, fonts_ok=True)
    MARKER.write_text("RobRunTurkceYama\nMode=AddressablesCache+FontGlyphs\n", encoding="utf-8")
    log("OK: kurulum tamam (Turkce font + metin) — ÖZELLEŞTİRME")
    log(f"hedef: {CACHE}")


def uninstall() -> None:
    if not CACHE.is_dir():
        log("Kurulu yama yok.")
        return
    for fn in list(CACHE.iterdir()):
        if fn.is_file():
            fn.unlink()
        elif fn.is_dir():
            shutil.rmtree(fn, ignore_errors=True)
    try:
        CACHE.rmdir()
    except OSError:
        pass
    log("OK: yama kaldirildi")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--kaldir", action="store_true")
    ap.add_argument("--font-only", action="store_true")
    ap.add_argument("--game", type=Path)
    args = ap.parse_args()
    try:
        if args.kaldir:
            uninstall()
            return 0
        if args.font_only:
            game = Path(args.game) if args.game else find_game_dir()
            return font_only(game)
        install(args.game)
        return 0
    except SystemExit as ex:
        code = ex.code
        if isinstance(code, str):
            print(code, file=sys.stderr, flush=True)
            return 1
        if isinstance(code, int):
            return code
        return 1
    except BaseException as ex:
        print(f"Kurulum hatasi: {type(ex).__name__}: {ex}", file=sys.stderr, flush=True)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
