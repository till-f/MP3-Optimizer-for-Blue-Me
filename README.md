# MP3 Optimizer and Playlist Manager for Blue&Me

This tool will fix (reformat) your MP3 files so that they are properly recognized by the Blue&Me unit in cars of Fiat, Alfa Romeo and Lancia. It removes unsupported ID3 tags and replaces special characters by their latinized counterpart (e.g. German "ä" becomes "ae" or cyrillic "Б" becomes "B"). This will fix the common issues that some or all MP3 files on the USB stick are not played.

The tool also supports playlist creation. You can quickly select the desired files and add them to the desired list(s) or remove files from existing lists. You can also reorder the files of a playlist easily.

![Screenshot of Main Window](README_img1.png?raw=true "Screenshot of Main Window")

## Download

1. Go to the [release page](https://github.com/till-f/MP3-Optimizer-for-Blue-Me/releases) and get the latest version.
2. Extract the archive.
3. Run BlueAndMeManager.exe.
	- *Note: You might be asked to install .NET Core, which is mandatory.*

## MP3 conversion

1. Copy your .mp3 files to the USB stick.
	- *Note: You can also copy them into an empty folder on your hard drive first.*
	- *Note: It is recommended to use a flat folder structure (any number of folders in the root directory but no additional subdirectories).*
	- *Note: If you already have .m3u playlists, you can also place them on the stick. All playlists must be in the root directory. Extended m3u playlists are not supported.*
2. Start the application and use the "..." button to select your USB stick or temporary folder from step 1. Alternatively, enter the corresponding path into the text field and click "Open/Refresh".
	- *Note: You may tick "Remove missing tracks from playlists" if you get an error that a playlist could not be loaded due to missing files. The missing files will then be removed from all playlists.*
3. Start the conversion of all files by pressing "Apply Blue&Me Fixes".
	- *Note: DO NOT RUN THIS ON YOUR ORIGINAL MEDIA LIBRARY! ID3 tags are removed and/or modified and the files may be moved/renamed.*
	- *Note: you can also run the conversion for just a few files by first selecting them in the list below.*
	- *Note: If playlists exists, they will automatically be adjusted to reflect any moved/renamed files.*

## Playlist creation

1. If not already done, follow steps 1 and 2 from above.
2. Create a new playlist by clicking the "Add Playlist" button.
3. Add the desired files to the playlist (use Ctrl+Click or Shift+Click for multi-selection).
	- *Note: Press `Return` to add the selected files to the selected playlist. Alternatively, use Drag&Drop or click on the "Add Selection to Playlist" button.*
	- *Note: Press `Delete` to remove the selected files from the selected playlist or click on the "Remove Selection from Playlist" button.*
4. Click on the "Reorder Playlist" button to change the order in which the files should be played.
	- *Note: Use Drag&Drop to move all selected files up or down.*
 
