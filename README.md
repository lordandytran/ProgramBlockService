# ProgramBlockService

ABOUT
Windows .NET Service blocks programs from being opened on a schedule by editing the registry

USAGE INSTRUCTIONS
1. Move executable and text files into a directory

2. Edit Schedule.txt and Program.txt accordingly.
    For Program.txt list all programs you want blocked on a new line. Just list the exe name, not the path.
    For Schedule.txt list allow and stop times under each day of the week one after the other.
    
    example (This means use of the program will be allowed 3:00am to 2:00pm and 5pm to 8pm on Monday):
        Monday
        3:00
        14:30
        17:00
        20:00

3. Run these commands in an elevated prompt, changing ServiceName to any name and PATH to the executable path:
    sc create ServiceName binPath="PATH"
    sc start ServiceName