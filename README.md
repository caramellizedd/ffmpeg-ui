# A simple GUI Wrapper for FFmpeg

## What is it, and how does it work?
- It's a .NET 10 C# GUI Program that wraps around a library called FFmpeg.NET which allow users to easily convert video files (or change their bitrates) without touching the terminal.
- It supports NVIDIA and AMD Acceleration (Intel are UNTESTED! Please post any issues in the issues tab)

## Features:
- Convert
  - Convert your video files to other extensions
  - Convert your video files with a specified CODEC
- Compressing
  - Compress your video files by changing the bitrate (MAY CAUSE VIDEO DEGREDATION IF SET TOO LOW!) or the codec

### **Required Libraries for Development:**
- FFMpegCore
- iNKORE.UI.WPF.Modern
