@echo off
ping 1.1.1.1 -n 1 -w 2000 > nul
%1 %2 %3 /E /Y
start "" %4