
#region Using Directives

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

#endregion

namespace MapTileGenerator
{
    /// <summary>
    /// Represents the main application.
    /// </summary>
    public class Program
    {
        #region Private Fields

        /// <summary>
        /// Contains letters that are sorted in alphabetic order.
        /// </summary>
        private static List<string> alphaRange = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the application.
        /// </summary>
        /// <param name="args">The application arguments.</param>
        public static void Main(string[] args)
        {
            // Prints out the usage information
            Console.WriteLine("=== Map Generator ===");
            Console.WriteLine("");
            Console.WriteLine("Generates a tile-based map of 256x256 pixel PNG tiles for the provided input image(s).");
            Console.WriteLine("The maximum zoom factor is automatically calculated based on the resolution of the input image(s).");
            Console.WriteLine("These tiles can be used by common tile provider to display the map.");
            Console.WriteLine("");
            Console.WriteLine("Requirements:");
            Console.WriteLine("* Input images must be played in a folder called 'source'.");
            Console.WriteLine("* Names of input images must start with A-Z for vertical position followed by 0-999 for horizontal position.");
            Console.WriteLine("* E.g. a 3x2 map's input images could be named A0.png, A1.png, A2.png, B0.png, B1.png, B2.png.");
            Console.WriteLine("* Images should overlay the following images by 1 pixel at the bottom and the right side.");
            Console.WriteLine("");
            Console.WriteLine("The output is put into a folder called 'target'.");

            // Initializes the map
            Map map = new Map();

            // Checks if the source folder exists
            if (!Directory.Exists("source"))
            {
                Console.WriteLine("Folder 'source' does not exist.");
                return;
            }
            
            // Gets all source map tiles
            List<string> sourceFileNames = Directory.GetFiles("source").ToList();

            // Gets the overlay value in pixels
            int overlap = 1;

            // Gets the maximum horizontal and vertical indexes for the source map tiles
            Dictionary<int, Dictionary<int, string>> sourceFilesDictionary = new Dictionary<int, Dictionary<int, string>>();
            int horizontalSourceMaximumIndex = 0;
            int verticalSourceMaximumIndex = 0;
            foreach (string sourceFileName in sourceFileNames)
            {
                Match match = Regex.Match(new FileInfo(sourceFileName).Name, "([A-Z])([0-9]+)(.*)");
                if (match.Groups.Count != 4)
                    continue;
                int horizontalIndex = int.Parse(match.Groups.ElementAt(2).Value);
                int verticalIndex = alphaRange.IndexOf(match.Groups.ElementAt(1).Value);
                horizontalSourceMaximumIndex = Math.Max(horizontalSourceMaximumIndex, horizontalIndex);
                verticalSourceMaximumIndex = Math.Max(verticalSourceMaximumIndex, verticalIndex);

                // Saves the source files in the dictionary
                if (!sourceFilesDictionary.ContainsKey(horizontalIndex))
                    sourceFilesDictionary.Add(horizontalIndex, new Dictionary<int, string>());
                sourceFilesDictionary[horizontalIndex].Add(verticalIndex, sourceFileName);
            }

            // Gets the resolution of the source map tiles
            int sourceTileWidth = 0;
            int sourceTileHeight = 0;
            using (Image sourceImage = Image.FromFile(sourceFilesDictionary.First().Value.First().Value))
            {
                sourceTileWidth = sourceImage.Width - overlap;
                sourceTileHeight = sourceImage.Height - overlap;
            }
            int sourceWidth = sourceTileWidth * (horizontalSourceMaximumIndex + 1);
            int sourceHeight = sourceTileHeight * (verticalSourceMaximumIndex + 1);

            // Prints out the basic information
            Console.WriteLine($"Source tile width: {sourceTileWidth} pixels");
            Console.WriteLine($"Source tile height: {sourceTileHeight} pixels");
            Console.WriteLine($"Horizontal number of tiles: {horizontalSourceMaximumIndex + 1}");
            Console.WriteLine($"Vertical number of tiles: {verticalSourceMaximumIndex + 1}");
            Console.WriteLine($"Source width: {sourceWidth} pixels");
            Console.WriteLine($"Target height: {sourceHeight} pixels");

            // Calculates the target size
            int targetTileSize = 256;
            int maximumHorizontalZoomFactor = (int)Math.Ceiling(Math.Log(1.0 * sourceWidth / targetTileSize, 2));
            int maximumVerticalZoomFactor = (int)Math.Ceiling(Math.Log(1.0 * sourceHeight / targetTileSize, 2));
            int maximumZoomFactor = Math.Max(maximumHorizontalZoomFactor, maximumVerticalZoomFactor);
            int horizontalTargetMaximumIndex = (int)(Math.Pow(2, maximumHorizontalZoomFactor) - 1);
            int verticalTargetMaximumIndex = (int)(Math.Pow(2, maximumVerticalZoomFactor) - 1);
            int targetWidth = targetTileSize * (horizontalTargetMaximumIndex + 1);
            int targetHeight = targetTileSize * (verticalTargetMaximumIndex + 1);
            map.MaximumZoomFactor = maximumZoomFactor;

            // Prints out the basic information
            Console.WriteLine($"Target tile size: {targetTileSize} pixels");
            Console.WriteLine($"Maximum horizontal zoom factor: {maximumHorizontalZoomFactor}");
            Console.WriteLine($"Maximum vertical zoom factor: {maximumVerticalZoomFactor}");
            Console.WriteLine($"Maximum zoom factor: {maximumZoomFactor}");
            Console.WriteLine($"Horizontal number of tiles (at maximum zoom factor): {horizontalTargetMaximumIndex + 1}");
            Console.WriteLine($"Vertical number of tiles (at maximum zoom factor): {verticalTargetMaximumIndex + 1}");
            Console.WriteLine($"Target width (at maximum zoom factor): {targetWidth} pixels");
            Console.WriteLine($"Target height (at maximum zoom factor): {targetHeight} pixels");

            // Stores the map information
            map.Width = targetWidth;
            map.Height = targetHeight;
            Directory.CreateDirectory("target");
            File.WriteAllText($"target{Path.DirectorySeparatorChar}map.json", JsonConvert.SerializeObject(map));

            // Prints out information
            Console.WriteLine("Creating blank files for maximum zoom factor...");

            // Creates the initial files for the maximum zoom layer
            Directory.CreateDirectory($"target{Path.DirectorySeparatorChar}{maximumZoomFactor}");
            using (Bitmap targetBitmap = new Bitmap(targetTileSize, targetTileSize))
            {
                for (int horizontalIndex = 0; horizontalIndex <= horizontalTargetMaximumIndex; horizontalIndex++)
                {
                    double horizontalProgress = (1.0 * horizontalIndex / (horizontalTargetMaximumIndex + 1));
                    for (int verticalIndex = 0; verticalIndex <= verticalTargetMaximumIndex; verticalIndex++)
                    {
                        double verticalProgress = (1.0 * verticalIndex / (verticalTargetMaximumIndex + 1));

                        // Prints out information
                        Console.WriteLine($"Creating blank files for maximum zoom factor: {(100.0 * (horizontalProgress + (verticalProgress / (horizontalTargetMaximumIndex + 1)))).ToString("0.00")} %");
                        
                        // Saves the file
                        targetBitmap.Save($"target{Path.DirectorySeparatorChar}{maximumZoomFactor}{Path.DirectorySeparatorChar}{horizontalIndex}-{verticalIndex}.png", ImageFormat.Png);
                    }
                } 
            }

            // Prints out information
            Console.WriteLine("Created blank files for maximum zoom factor.");

            // Gets the offset for the maximum zoom factor
            int horizontalOffset = (int)Math.Floor(((targetWidth - sourceWidth) / 2.0));
            int verticalOffset = (int)Math.Floor(((targetHeight - sourceHeight) / 2.0));

            // Prints out information
            Console.WriteLine("Filling maximum zoom factor...");

            // Cycles over all source files and creates the maximum zoom layer
            for (int horizontalIndex = 0; horizontalIndex <= horizontalSourceMaximumIndex; horizontalIndex++)
            {
                double horizontalProgress = (1.0 * horizontalIndex / (horizontalSourceMaximumIndex + 1));
                for (int verticalIndex = 0; verticalIndex <= verticalSourceMaximumIndex; verticalIndex++)
                {
                    double verticalProgress = (1.0 * verticalIndex / (verticalSourceMaximumIndex + 1));
                
                    // Prints out information
                    Console.WriteLine($"Filling maximum zoom factor: {(100.0 * (horizontalProgress + (verticalProgress / (horizontalSourceMaximumIndex + 1)))).ToString("0.00")} %");

                    // Checks if a file for the indexes actually exists
                    if (!sourceFilesDictionary.ContainsKey(horizontalIndex) || !sourceFilesDictionary[horizontalIndex].ContainsKey(verticalIndex))
                        continue;

                    // Loads the source image
                    using (Image sourceImage = Image.FromFile(sourceFilesDictionary[horizontalIndex][verticalIndex]))
                    {
                        // Gets the absolute target coordinates for the source
                        int targetX = horizontalOffset + (horizontalIndex * sourceTileWidth);
                        int targetY = verticalOffset + (verticalIndex * sourceTileHeight);

                        int currentSourceX = 0;
                        while (currentSourceX < sourceTileWidth)
                        {
                            // Determines the horizontal tile position for the target
                            int currentTargetX = targetX + currentSourceX;
                            int targetTileHorizontalIndex = (int)(Math.Floor(1.0 * currentTargetX / targetTileSize));

                            // Determines offset and the width in the target
                            int offsetXInTile = currentTargetX - (targetTileHorizontalIndex * targetTileSize);
                            int widthInTile = Math.Min(targetTileSize - offsetXInTile, sourceTileWidth - currentSourceX);

                            int currentSourceY = 0;
                            while (currentSourceY < sourceTileHeight)
                            {
                                // Prints out information
                                Console.WriteLine($"Filling maximum zoom factor: {(100.0 * (horizontalProgress + (verticalProgress / (horizontalSourceMaximumIndex + 1)))).ToString("0.00")} % | X: {currentSourceX} | Y: {currentSourceY}");

                                // Determines the vertical tile position for the target
                                int currentTargetY = targetY + currentSourceY;
                                int targetTileVerticalIndex = (int)(Math.Floor(1.0 * currentTargetY / targetTileSize));

                                // Determines offset and the height in the target
                                int offsetYInTile = currentTargetY - (targetTileVerticalIndex * targetTileSize);
                                int heightInTile = Math.Min(targetTileSize - offsetYInTile, sourceTileHeight - currentSourceY);

                                // Draws the (partial) image to the maximum zoom factor image
                                File.Delete($"target{Path.DirectorySeparatorChar}{maximumZoomFactor}{Path.DirectorySeparatorChar}{targetTileHorizontalIndex}-{targetTileVerticalIndex}.png");
                                using (Bitmap targetBitmap = new Bitmap(targetTileSize, targetTileSize))
                                {
                                    using (Graphics targetGraphics = Graphics.FromImage(targetBitmap))
                                    {
                                        targetGraphics.DrawImage(sourceImage, new Rectangle(offsetXInTile, offsetYInTile, widthInTile, heightInTile), new Rectangle(currentSourceX, currentSourceY, widthInTile, heightInTile), GraphicsUnit.Pixel);
                                    }
                                    targetBitmap.Save($"target{Path.DirectorySeparatorChar}{maximumZoomFactor}{Path.DirectorySeparatorChar}{targetTileHorizontalIndex}-{targetTileVerticalIndex}.png", ImageFormat.Png);
                                }
                                
                                currentSourceY = currentSourceY + heightInTile;
                            }

                            currentSourceX = currentSourceX + widthInTile;
                        }
                    }
                }
            }

            // Prints out information
            Console.WriteLine("Filled maximum zoom factor.");

            // Creates the other zoom factors
            for (int zoomFactor = maximumZoomFactor - 1; zoomFactor >= 1; zoomFactor--)
            {
                Directory.CreateDirectory($"target{Path.DirectorySeparatorChar}{zoomFactor}");

                // Prints out information
                Console.WriteLine($"Filling zoom factor {zoomFactor}...");

                // Decreases the indexes
                horizontalTargetMaximumIndex = horizontalTargetMaximumIndex / 2;
                verticalTargetMaximumIndex = verticalTargetMaximumIndex / 2;

                // Merges four tiles into one with each index
                for (int horizontalIndex = 0; horizontalIndex <= horizontalTargetMaximumIndex; horizontalIndex++)
                {
                    double horizontalProgress = (1.0 * horizontalIndex / (horizontalTargetMaximumIndex + 1));
                    for (int verticalIndex = 0; verticalIndex <= verticalTargetMaximumIndex; verticalIndex++)
                    {
                        double verticalProgress = (1.0 * verticalIndex / (verticalTargetMaximumIndex + 1));

                        // Loads the four source images
                        using (Bitmap targetBitmap = new Bitmap(targetTileSize, targetTileSize))
                        {
                            using (Graphics targetGraphics = Graphics.FromImage(targetBitmap))
                            {
                                using (Image sourceImage = Image.FromFile($"target{Path.DirectorySeparatorChar}{(zoomFactor + 1)}{Path.DirectorySeparatorChar}{(horizontalIndex * 2)}-{(verticalIndex * 2)}.png"))
                                {
                                    targetGraphics.DrawImage(sourceImage, new Rectangle(0, 0, targetTileSize / 2, targetTileSize / 2), new Rectangle(0, 0, targetTileSize, targetTileSize), GraphicsUnit.Pixel);
                                }
                                using (Image sourceImage = Image.FromFile($"target{Path.DirectorySeparatorChar}{(zoomFactor + 1)}{Path.DirectorySeparatorChar}{(horizontalIndex * 2)}-{(verticalIndex * 2) + 1}.png"))
                                {
                                    targetGraphics.DrawImage(sourceImage, new Rectangle(0, targetTileSize / 2, targetTileSize / 2, targetTileSize / 2), new Rectangle(0, 0, targetTileSize, targetTileSize), GraphicsUnit.Pixel);
                                }
                                using (Image sourceImage = Image.FromFile($"target{Path.DirectorySeparatorChar}{(zoomFactor + 1)}{Path.DirectorySeparatorChar}{(horizontalIndex * 2) + 1}-{(verticalIndex * 2)}.png"))
                                {
                                    targetGraphics.DrawImage(sourceImage, new Rectangle(targetTileSize / 2, 0, targetTileSize / 2, targetTileSize / 2), new Rectangle(0, 0, targetTileSize, targetTileSize), GraphicsUnit.Pixel);
                                }
                                using (Image sourceImage = Image.FromFile($"target{Path.DirectorySeparatorChar}{(zoomFactor + 1)}{Path.DirectorySeparatorChar}{(horizontalIndex * 2) + 1}-{(verticalIndex * 2) + 1}.png"))
                                {
                                    targetGraphics.DrawImage(sourceImage, new Rectangle(targetTileSize / 2, targetTileSize / 2, targetTileSize / 2, targetTileSize / 2), new Rectangle(0, 0, targetTileSize, targetTileSize), GraphicsUnit.Pixel);
                                }
                                
                                // Saves the file
                                targetBitmap.Save($"target{Path.DirectorySeparatorChar}{zoomFactor}{Path.DirectorySeparatorChar}{horizontalIndex}-{verticalIndex}.png", ImageFormat.Png);
                            }
                        }

                        // Prints out information
                        Console.WriteLine($"Filling zoom factor {zoomFactor}: {(100.0 * (horizontalProgress + (verticalProgress / (horizontalTargetMaximumIndex + 1)))).ToString("0.00")} %");
                    }
                }

                // Prints out information
                Console.WriteLine($"Filled zoom factor {zoomFactor}.");
            }
        }

        #endregion
    }
}
