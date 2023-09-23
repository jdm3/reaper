# reaper

*reaper* is a command line tool for Windows that looks for processes with a particular name, and kills them if they run for too long.

```
usage: reaper.exe [options] NAME LIFESPAN
options:
    NAME        The name of the target process.
    LIFESPAN    The maximum life span for target processes in seconds (default=0).
    --wait      Process any existing processes, and then keep running to watch for new processes.
```
