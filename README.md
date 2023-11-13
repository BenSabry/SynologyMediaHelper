# Synology Media Helper
<b>*Synology Media Helper*</b> is a tool for fixing media files <b>*CreationDate*</b> to be ordered properly on your <b>*Synology Photos App*</b> or any other App in case you uploaded your old Media and found it NOT organized properly.

# Downloads
<b>*Last release*</b> [v0.2.0-alpha](https://github.com/BenSabry/SynologyMediaHelper/releases/tag/v0.2.0-alpha)<br />
<b>*All releases*</b> [releases](https://github.com/BenSabry/SynologyMediaHelper/releases)

# Recommendations
a. <b>*BACKUP*</b> your media files first (you may use <b>*Synology Snapshot Replication*</b>)<br />
b. Connect your PC to <b>*SynologyNAS*</b> using Cable not WIFI for best performance<br />
c. Increase the <b>*TasksCount*</b> in <b>*AppSettings.json*</b> (recommended: 2, best: <b>*CPU Cores*</b> count)<br />

# How to use
1. From your Windows PC open photos library directory on <b>*Synology*</b> from <b>*Windows Network (SMB)*</b><br />
2. Add the path of your library to <b>*Sources*</b> in <b>*AppSettings.json*</b> file<br />
3. run the executable <b>*SynologyMediaHelper.exe*</b> and wait<br />

# How it works
1. Scan library files and directories added to <b>*Sources*</b> in <b>*AppSettings.json*</b><br />
2. Check if the file already has <b>*CreationDate*</b><br />
&nbsp;&nbsp;&nbsp;2.1. if so: Move the file to proper directory <b>*MediaLibrary\Year\Month\File.*</b><br />
&nbsp;&nbsp;&nbsp;2.2. else:<br />
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.2.1. Extract date from filename if any and all dates from file info and choose the oldest<br />
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.2.2. Update the file info <b>*CreationDate*</b> (after creating temp file as Backup)<br />
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.2.3. Attempt to fix file info (like info duplications ...etc)<br />
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.2.4. Move the file to proper directory <b>*MediaLibrary\Year\Month\File.*</b><br /><br />
3. Clear temp files (created as backup incase App/Windows closed improperly)<br />
4. Delete empty directories<br /><br />

Now just wait and your <b>*Synology*</b> will reindex media files and reorder them again based on updated info

# Example of problems can be fixed
[<b>*Synology Photos: Not Using Taken Date*</b>](https://www.reddit.com/r/synology/comments/kgy604/synology_photos_not_using_taken_date/)<br />
[<b>*Synology Photos: organizes everything by modified date instead of creation date*</b>](https://www.reddit.com/r/synology/comments/120jsvk/synology_photos_organizes_everything_by_modified/)<br />
[<b>*Synology Photos: Indexing photos/videos with wrong date*</b>](https://www.reddit.com/r/synology/comments/qj9wya/synology_photos_indexing_photosvideos_with_wrong/)<br />
[<b>*Synology Photos: Best practice for photos with no taken date*</b>](https://www.reddit.com/r/synology/comments/rn5cvm/best_practice_for_photos_with_no_taken_date/)<br />

# Tech/Tools used
<b>*DotNET*</b>: is the free, open-source, cross-platform framework for building modern apps and powerful cloud services.<br />
<b>*ExifTool*</b>: is a customizable set of Perl modules plus a full-featured command-line application for reading and writing meta information in a wide variety of files.<br />
