# Synology Media Helper
<b>*Synology Media Helper*</b> is a tool for fixing media files <b>*CreationDate*</b> to be ordered successfully on your <b>*Synology Photos App*</b> in case you uploaded your old Media and found it not organized properly.

# Downloads
<b>*Last release*</b> [v0.2.0-alpha](https://github.com/BenSabry/SynologyMediaHelper/releases/tag/v0.2.0-alpha)<br />
<b>*All releases*</b> [releases](https://github.com/BenSabry/SynologyMediaHelper/releases)

# How to use
1. From your Windows PC open Photos Library directory on Synology from <b>*Windows Network (SMB)*</b><br />
2. Add the Path of your library to <b>*Sources*</b>in <b>*AppSettings.json*</b>file<br />
3. run the executable <b>*SynologyMediaHelper.exe*</b> and watch<br />

# How it works
1. Scan library files and directories added to <b>Sources</b> in <b>AppSettings.json</b><br />
2. Check if the file already has <b>*CreationDate*</b><br />
&nbsp;&nbsp;&nbsp;2.1. if so: Move the File to Directory based on the Date <b>*MediaLibrary\Year\Month\File.*</b><br />
&nbsp;&nbsp;&nbsp;2.2. else:<br />
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.2.1. Extract Date from filename if any and All Dates in File info and choose the oldest Date<br />
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.2.2. Update the File Info <b>*CreationDate*</b> (after creating temp file as Backup)<br />
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2.2.3. Move the File to Directory based on the Date <b>*MediaLibrary\Year\Month\File.*</b><br />
3. Clear Temp files -Created as Backup incase Program/Windows closed while working to prevent any loss<br />
4. Delete Empty Directories<br /><br />
Now just wait and your Synology will reindex media files and reorder them again based on new Dates

# Example of problems can be fixed
<b>*Synology Photos Not Using Taken Date*</b><br />
https://www.reddit.com/r/synology/comments/kgy604/synology_photos_not_using_taken_date/<br />
<b>*Synology photos organizes everything by modified date instead of creation date*</b><br />
https://www.reddit.com/r/synology/comments/120jsvk/synology_photos_organizes_everything_by_modified/<br />
<b>*Synology Photos: Indexing photos/videos with wrong date*</b><br />
https://www.reddit.com/r/synology/comments/qj9wya/synology_photos_indexing_photosvideos_with_wrong/<br />
<b>*Best practice for photos with no taken date*</b><br />
https://www.reddit.com/r/synology/comments/rn5cvm/best_practice_for_photos_with_no_taken_date/<br />

# Tech/Tools used
<b>*DotNET*</b>: is the free, open-source, cross-platform framework for building modern apps and powerful cloud services.<br />
<b>*ExifTool*</b>: is a customizable set of Perl modules plus a full-featured command-line application for reading and writing meta information in a wide variety of files.<br />
