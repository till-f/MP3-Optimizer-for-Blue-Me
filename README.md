# MP3 Optimizer and Playlist Manager for Blue&Me

This software tool is intended to fix (reformat) MP3 files so that they are properly recognized by the Blue&Me unit in cars of Fiat, Alfa Romeo and Lancia. It removes unsupported ID3 tags and replaces special characters by their latinized counterpart (e.g. German "ä" becomes "ae" or cyrillic "Б" becomes "B").

The tool also supports playlist creation. You can quickly select the desired files and add them to the desired list(s) or remove files from existing lists. You can also reorder the files of a playlist easily.

## MP3 conversion

1. Copy the desired files to your USB stick 
	- *Note: You can also copy them into an empty folder on your hard drive first.*
	- *Note: It is recommended to use a flat folder structure (any number of folders* in the root directory but no additional subdirectories).
	- *Note: If you already have .m3u playlists, you can also place them there. All playlists must be in the root directory. Extended m3u playlists are not supported.*
2. Start the application and use the "..." button to select your USB stick or temporary folder from step 1. Alternatively, enter the corresponding path into the text field and click "Open/Refresh".
3. Start the conversion of all files by pressing "Apply Blue&Me Fixes".
	- *Note: DO NOT RUN THE CONVERSION ON YOUR ORIGINAL MEDIA LIBRARY! All ID3 tags are modified and files may be moved/renamed.*
	- *Note: you can also run the conversion for just a few files by first selecting them in the list below.*
	- *If playlists have been loaded, they will automatically be adjusted to reflect any renamed files.*

## Playlist creation

1. If not already done, follow steps 1 and 2 from above.
2. Create a new playlist by clicking the "Add Playlist" button.
3. Add the desired files to the playlist (use Ctrl+Click or Shift+Click for multi-selection)
	- *Note: Press `Return` to add the selected files to the selected playlist. Alternatively, use Drag&Drop or click on the "Add Selection to Playlist" button.*
	- *Note: Press `Delete` to remove the selected files from the selected playlist or click on the "Remove Selection from Playlist" button.*
4. Click on the "Reorder Playlist" button to change the order in which the files should be played.
	- *Note: Use Drag&Drop to move all selected files up or down.*