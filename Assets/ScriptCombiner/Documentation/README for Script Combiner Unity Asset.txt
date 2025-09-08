README for Script Combiner Unity Asset
Installation
1. Download the ScriptCombiner.unitypackage from the Asset Store
2. Open your Unity project (2019.4 or newer recommended)
3. Navigate to Assets > Import Package > Custom Package
4. Select the downloaded ScriptCombiner.unitypackage
5. Ensure all files are checked and click "Import"
Usage
Basic Operation
1. Open the Script Combiner window: Tools > Combine Scripts (With Selection)
2. Select encoding (UTF-8, ANSI, or Windows-1251)
3. Add files/folders using:
   1. Add Selected in Project: Select assets in Project view first
   2. Add Folder: Browse for folders outside your project
4. View real-time statistics of selected content
5. Click Combine Selected Scripts to generate the combined file
Features
* Encoding Support: Multiple encoding options for compatibility
* Statistics: Real-time analysis of selected scripts
* Batch Processing: Combine entire folders recursively
* Smart Detection: Automatic encoding detection for source files
Advanced Usage
For large projects, consider:
1. Combining scripts by category (UI, Gameplay, etc.)
2. Using different encodings for specific localization needs
3. Reviewing statistics to identify code complexity patterns
Documentation in Unity Package Manager
This package includes integrated documentation visible in the Unity Package Manager:
1. Open Package Manager (Window > Package Manager)
2. Select "My Assets" or "In Project" view
3. Locate Script Combiner in the list
4. View documentation in the details panel
Troubleshooting
Common Issues
1. Encoding Problems: 
   1. Use UTF-8 for maximum compatibility
   2. Check source file encodings if characters appear corrupted
2. Large Files:
   1. The tool can handle large projects but may take time for 1000+ files
   2. Consider combining in batches for very large projects
3. Permission Errors:
   1. Ensure write permissions in the target directory
   2. Restart Unity as administrator if needed
Support
For additional support:
1. Check the integrated documentation
2. Contact redikan15@gmail.com
License Information
This asset is licensed under the Unity Asset Store EULA. See the licenses.pdf file for complete details.
Release Notes
Version 1.0.0
1. Initial release
2. Basic script combining functionality
3. Encoding support (UTF-8, ANSI, Windows-1251)
4. Statistical analysis
5. Unity 2019.4+ compatibility