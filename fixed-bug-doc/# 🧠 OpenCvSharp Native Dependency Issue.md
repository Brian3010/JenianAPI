# 🧠 OpenCvSharp Native Dependency Issue (Azure Container Apps)

## 📌 Overview

This document describes a production issue where **OpenCvSharp failed to load in Azure Container Apps** due to missing native Linux dependencies.

It also includes a **reusable troubleshooting guide** for any future native library issues in Docker.

---

## 🚨 Issue Summary

### Symptom

```txt
System.DllNotFoundException: Unable to load shared library 'OpenCvSharpExtern'
```

With details like:

```txt
libjpeg.so.8: cannot open shared object file
libgtk-3.so.0: cannot open shared object file
libfreetype.so.6: cannot open shared object file
```

---

## 🧩 Root Cause

```csharp
// OpenCvSharp is NOT fully managed
// It depends on native C++ OpenCV binaries (.so files)
```

Your app loads:

```
/app/libOpenCvSharpExtern.so
```

But that native file depends on system libraries:

```
libjpeg
libtiff
libgtk
libpango
libcairo
libX11
...
```

### Problem

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
```

This base image is **minimal Debian**, so required libraries are missing.

---

## 🔍 Debugging Process

### 1. Verify native file exists

```bash
find /app -name "*OpenCvSharp*"
```

Result:

```
/app/libOpenCvSharpExtern.so
```

✅ File exists → not a publish issue

---

### 2. Inspect dependencies

```bash
ldd /app/libOpenCvSharpExtern.so
```

### Before fix

```
libjpeg.so.8 => not found
libgtk-3.so.0 => not found
libpango-1.0.so.0 => not found
```

### Interpretation

```csharp
// Native library exists
// but cannot load due to missing dependencies
```

---

## 🛠️ Solution

### Install required system libraries in Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgomp1 \
    libglib2.0-0 \
    libfreetype6 \
    libharfbuzz0b \
    libgtk-3-0 \
    libpangocairo-1.0-0 \
    libpango-1.0-0 \
    libatk1.0-0 \
    libcairo-gobject2 \
    libcairo2 \
    libgdk-pixbuf-2.0-0 \
    libdrm2 \
    libatomic1 \
    libx11-6 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Jenian.API.dll"]
```

---

## 🔗 Dependency Mapping

| Missing `.so`     | Debian Package |
| ----------------- | -------------- |
| libfreetype.so.6  | libfreetype6   |
| libharfbuzz.so.0  | libharfbuzz0b  |
| libgtk-3.so.0     | libgtk-3-0     |
| libpango-1.0.so.0 | libpango-1.0-0 |
| libcairo.so.2     | libcairo2      |
| libX11.so.6       | libx11-6       |

---

## ✅ Verification

```bash
ldd /app/libOpenCvSharpExtern.so
```

### After fix

```
libgtk-3.so.0 => /lib/x86_64-linux-gnu/libgtk-3.so.0
libfreetype.so.6 => /lib/x86_64-linux-gnu/libfreetype.so.6
```

✅ No more `=> not found`

---

## 🧠 Lessons Learned

### 1. Not all NuGet packages are self-contained

```csharp
// Managed libraries → safe
// Native-backed libraries → require OS dependencies
```

---

### 2. Docker is minimal

```csharp
// Local machine ≠ Docker container
```

---

### 3. `ldd` is critical

```bash
ldd <library>
```

---

### 4. Error messages are misleading

```txt
cannot open shared object file
```

Often means:

```txt
dependency missing, not file missing
```

---

### 5. Docker cache hides fixes

```yaml
no-cache: true
```

---

### 6. Always debug inside container

```bash
docker run -it <image> /bin/bash
```

---

### 7. Native dependency trees change

Different runtime packages → different `.so` requirements

---

## 🧠 Mental Model

```
C# Code
   ↓
OpenCvSharp.dll
   ↓
libOpenCvSharpExtern.so
   ↓
Linux system libraries (.so)
```

---

## 🧰 Troubleshooting Native Libraries (Reusable Template)

### Step 0 — Identify error

```txt
DllNotFoundException
cannot open shared object file
```

---

### Step 1 — Check file exists

```bash
find /app -name "*.so"
```

---

### Step 2 — Run ldd

```bash
ldd <file>
```

---

### Step 3 — Look for missing libs

```
=> not found
```

---

### Step 4 — Map to packages

```csharp
libXYZ.so → libxyz
```

---

### Step 5 — Install via apt

```dockerfile
RUN apt-get install <packages>
```

---

### Step 6 — Rebuild without cache

```yaml
no-cache: true
```

---

### Step 7 — Verify again

```bash
ldd <file>
```

---

## ⚠️ Common Pitfalls

* File exists but dependencies missing
* Works locally but fails in Docker
* Wrong package version
* Docker cache not invalidated
* Debugging outside container

---

## 🧩 Decision Tree

```csharp
if (file missing)
    → fix build/publish

else if (ldd shows not found)
    → install dependencies

else
    → check architecture/runtime mismatch
```

---

## 📦 Useful Commands

```bash
find /app -name "*.so"
ldd <file>
docker run -it <image> /bin/bash
cat /etc/os-release
```

---

## 📌 Final Conclusion

```csharp
// Root cause:
Missing native Linux dependencies

// Fix:
Install required system libraries in Docker

// Status:
Resolved ✅
```

---

## 🚀 Rule of Thumb

```csharp
// Always remember:
DllNotFoundException = dependency problem (most of the time)
```
