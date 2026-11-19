# tr_cache_worker

Python tool that installs Turkish Addressables cache entries for ROB RUN.

## Build

```powershell
cd tools/tr_worker
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
pyinstaller tr_cache_worker.spec
copy .\dist\tr_cache_worker.exe ..\..\installer\PatchData\
```

## Run (debug)

```powershell
python install_cache.py --game "D:\Steam\steamapps\common\ROB RUN"
python install_cache.py --kaldir
```

Writes under `%USERPROFILE%\AppData\LocalLow\Kefir\ROB RUN\com.unity.addressables\`.
