# GL Map Packer 2.1

**Download:** `GLMapPacker-2.1.zip` (app, Maya `Injector.py`, quick start).
Requires Windows 10 or 11 with .NET Framework 4.8.

## New

- **Convert tab** with three map conversions and a live before/after preview:
  - **Normal → Height**: whole-image FFT Poisson integration, with height intensity, mid-point baseline, contrast and surface blur; OpenGL or DirectX input.
  - **Height → Normal**: Sobel gradient, with strength, smoothing and invert; OpenGL or DirectX output.
  - **Height → Ambient Occlusion**: horizon-based AO (HBAO), with radius, depth, intensity, contrast and quality.
  - Normal → Height **integration modes**: *Tileable* for tiling textures; *UV islands* (with a UV mask or ID map), which solves each island on its own and keeps broad shapes; and *Mesh-aware*, which reads the low-poly OBJ and stitches UV seams so heights meet across islands, even rotated ones. Plus a *Flatten broad shapes* slider for detail-only height.
  - Float32 processing and **true 16-bit PNG** input and output. The 8-bit / 16-bit toggle follows the loaded file.
  - Output resolution: source size, or 512 to 4096 px.
- **New interface**: card layout, segmented tabs, custom dropdowns and text fields, one accent color in light and dark themes, toasts and a status bar.
- **Better previews**: wheel zoom, pan, Fit, R/G/B/A channel view, size and zoom readout, a draggable before/after divider, and drag-and-drop drop zones.
- **High-DPI support**, a dark title bar on Windows 10 and 11, and a new app icon.

## Fixes

- Dropping an image directly onto a Pack slot's preview now loads it.

## Notes

- Pack, Unpack and Utility still work at 8 bits per channel.
- AO runs on the CPU, in about 1 to 2 s at 1024 px and roughly 10 to 20 s for a 4K save.
