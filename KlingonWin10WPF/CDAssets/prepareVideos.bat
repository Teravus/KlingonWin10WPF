.\ffmpeg\ffmpeg.exe -y -itsoffset 1.0 -i MAIN_1.avi -i MAIN_1.avi -map 0:0 -map 1:1 -acodec copy -vcodec copy MAIN_1X.avi
.\ffmpeg\ffmpeg.exe -y -itsoffset 1.0 -i MAIN_2.avi -i MAIN_2.avi -map 0:0 -map 1:1 -acodec copy -vcodec copy MAIN_2X.avi
.\ffmpeg\ffmpeg.exe -y -itsoffset 1.0 -i SS_1.avi -i SS_1.avi -map 0:0 -map 1:1 -acodec copy -vcodec copy SS_1X.avi
.\ffmpeg\ffmpeg.exe -y -itsoffset 1.0 -i SS_2.avi -i SS_2.avi -map 0:0 -map 1:1 -acodec copy -vcodec copy SS_2X.avi