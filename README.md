# map-tile-generator

Generates tiles (for tile-provider based map controls) from input maps.

## Usage

Generates a tile-based map of 256x256 pixel PNG tiles for the provided input image(s).
The maximum zoom factor is automatically calculated based on the resolution of the input image(s).
These tiles can be used by common tile provider to display the map.

## Requirements

* Input images must be played in a folder called 'source'.
* Names of input images must start with A-Z for vertical position followed by 0-999 for horizontal position.
* E.g. a 3x2 map's input images could be named A0.png, A1.png, A2.png, B0.png, B1.png, B2.png.
* Images should overlay the following images by 1 pixel at the bottom and the right side.

The output is put into a folder called 'target'.

## Build

Run `dotnet restore`,  `dotnet build` and `dotnet run` to execute the program.
