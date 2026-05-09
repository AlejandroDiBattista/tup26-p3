on run argv
  set inputPath to POSIX file (item 1 of argv)
  set outputPath to POSIX file (item 2 of argv)
  tell application "Microsoft Word"
    activate
    open inputPath
    set docRef to active document
    save as docRef file name outputPath file format format PDF
    close docRef saving no
  end tell
end run
