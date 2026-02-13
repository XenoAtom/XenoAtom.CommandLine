# Does not work well on Windows, so you need to run it from Linux / macOS

# Output format (pick one)
Output multi-demo.gif

# Visuals (adjust to taste)
Set FontSize 20
Set Width 1200
Set Height 980
Set TypingSpeed 80ms
#Set WaitTimeout 10s

Sleep 200ms

# 1) multi.exe --help
Type "./multi --help"
Enter
Sleep 1s
Type "clear"
Enter

# 2) --markup --help
Type "./multi --markup --help"
Enter
Sleep 1s
Type "clear"
Enter

# 3) --visual --help
Type "./multi --visual --help"
Enter
Sleep 1s
Type "clear"
Enter

# 4) hello ...
Type "./multi hello --name Test --age 10 file1.txt file2.txt"
Enter
Sleep 500ms
Type "clear"
Enter

# 5) invalid options (no clear between them, per your script)
Type "./multi --invalid-option"
Enter
Sleep 200ms
Type "./multi --markup --invalid-option"
Enter
Sleep 200ms
Type "./multi --visual --invalid-option"
Enter
Sleep 1s
