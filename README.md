<h1 align="center">Jellyfin NFO Chapters Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org/">Jellyfin Project</a></h3>

<div align="center">
<img alt="Logo" src="viperinius-plugin-nfochapters.png" />
<br>
<br>
<a href="https://github.com/Viperinius/jellyfin-plugin-nfo-chapters">
<img alt="GPL-3.0 license" src="https://img.shields.io/github/license/Viperinius/jellyfin-plugin-nfo-chapters" />
</a>
<a href="https://github.com/Viperinius/jellyfin-plugin-nfo-chapters/releases">
<img alt="Current release" src="https://img.shields.io/github/release/Viperinius/jellyfin-plugin-nfo-chapters" />
</a>
</div>

## About
This plugin extends the NFO parser for your Movies. You can specify your own chapters in the NFO with the `<chapters>` tag (detailed description [below](#how-to)), which will get picked up by the plugin during a library scan.

This can be beneficial in the following cases:
- You cannot or do not want do modify the movie sources to have embedded chapters
- You want to supply custom images for chapters by specifying a path somewhere on the filesystem
- You want to have the extracted chapter images saved in a specific location

## Installation

Link to the repository manifest to get the plugin to show up in your catalogue:
```
https://raw.githubusercontent.com/Viperinius/jellyfin-plugins/master/manifest.json
```

[See the official documentation for install instructions](https://jellyfin.org/docs/general/server/plugins/index.html#installing).

## How to
To add your own chapters, open the NFO of a movie and add the tag `<chapters>` inside the `<movie>`, e. g.:
```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<movie>
    <!-- ... -->
    <chapters>
    </chapters>
</movie>
```
Next, add a `<chapter>` tag for every chapter. It needs two attributes and optionally a content:
- attribute `name`: The chapter name that should be displayed.
- attribute `start`: The timestamp in seconds at which the chapter starts.
- content: The path where the chapter image can be found or should be saved. If this is empty, no image will be set. The name and path of the image can be chosen freely (as long as Jellyfin has access to it, of course).

Example:
```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<movie>
    <!-- ... -->
    <chapters>
        <!-- a chapter with "full information" -->
        <chapter name="Test Chapter @10min"
                 start="600">
            /media/my_movie_library/my_movie_title/.chapters/chapter_image_600.jpg
        </chapter>
        <!-- a chapter without any image -->
        <chapter name="2mins later"
                 start="720">
        </chapter>
    </chapters>
</movie>
```

In the plugin settings you can decide whether or not to extract chapter images and if this should be done after a library scan or as a scheduled task.
