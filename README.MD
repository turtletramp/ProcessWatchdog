# ProcessWatchdog

ProcessWatchdog is a small tool that reads a config file and monitors the processes listed in it. The process from the config gets started as soon as ProcessWatchdog starts and it will attempt to restart it in case the process exits.
It is a simple tool that can be used to ensure that a process is always running, and it can be useful for keeping a background, userspace process active in case it crashes or is killed.

## Intended use case

The intended use case is whenever a Windows Service cannot be used for whatever reason and the process in question must run in user space in the background.

## Features

- Monitors processes listed in a config file
- Restarts processes if they exit
- Supports multiple processes
- Reads config at start, first trying to read from %appdata% and second if not found from working directory `ProcessWatchdog.config.json`
