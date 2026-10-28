/* fxcshim : stands in for "dotnet c:\fxccs.dll" that MonoGame's WineHelper runs inside Wine.
   Usage (from WineHelper.RunFxc2): dotnet.exe c:\fxccs.dll <src> <function> <profile> <flags> <displayPath...> <dst>
   Calls D3DCompile from the real d3dcompiler_47.dll and writes the bytecode to <dst>. */
#define COBJMACROS
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <d3dcommon.h>

typedef HRESULT (WINAPI *D3DCompileFn)(LPCVOID, SIZE_T, LPCSTR, const D3D_SHADER_MACRO *, ID3DInclude *,
                                       LPCSTR, LPCSTR, UINT, UINT, ID3DBlob **, ID3DBlob **);

static void print_blob(ID3DBlob *b)
{
    if (!b) return;
    const char *p = (const char *)ID3D10Blob_GetBufferPointer(b);
    SIZE_T n = ID3D10Blob_GetBufferSize(b);
    fwrite(p, 1, n, stdout);
    fputc('\n', stdout);
}

int main(int argc, char **argv)
{
    int i = 1;
    if (argc > 1 && strstr(argv[1], "fxccs") != NULL) i = 2;   /* skip "c:\fxccs.dll" */
    if (argc - i < 6) { printf("fxcshim: expected <src> <function> <profile> <flags> <display> <dst>\n"); return 2; }

    const char *src = argv[i];
    const char *func = argv[i + 1];
    const char *profile = argv[i + 2];
    UINT flags = (UINT)strtoul(argv[i + 3], NULL, 10);
    const char *dst = argv[argc - 1];

    /* the display path is everything between flags and dst (it is not quoted by WineHelper) */
    char display[4096] = "";
    for (int k = i + 4; k < argc - 1; k++) {
        if (display[0]) strncat(display, " ", sizeof(display) - strlen(display) - 1);
        strncat(display, argv[k], sizeof(display) - strlen(display) - 1);
    }

    FILE *f = fopen(src, "rb");
    if (!f) { printf("fxcshim: cannot open source %s\n", src); return 3; }
    fseek(f, 0, SEEK_END); long len = ftell(f); fseek(f, 0, SEEK_SET);
    char *data = (char *)malloc(len + 1);
    fread(data, 1, len, f); fclose(f); data[len] = 0;

    HMODULE dll = LoadLibraryA("d3dcompiler_47.dll");
    if (!dll) { printf("fxcshim: cannot load d3dcompiler_47.dll (error %lu)\n", GetLastError()); return 4; }
    D3DCompileFn compile = (D3DCompileFn)GetProcAddress(dll, "D3DCompile");
    if (!compile) { printf("fxcshim: D3DCompile not found\n"); return 5; }

    ID3DBlob *code = NULL, *errors = NULL;
    HRESULT hr = compile(data, (SIZE_T)len, display[0] ? display : src, NULL, NULL, func, profile, flags, 0, &code, &errors);
    print_blob(errors);
    if (FAILED(hr) || !code) { printf("fxcshim: D3DCompile failed 0x%08lx\n", (unsigned long)hr); return 1; }

    FILE *o = fopen(dst, "wb");
    if (!o) { printf("fxcshim: cannot write %s\n", dst); return 6; }
    fwrite(ID3D10Blob_GetBufferPointer(code), 1, ID3D10Blob_GetBufferSize(code), o);
    fclose(o);
    return 0;
}
