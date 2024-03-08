# Keyguard

A Windows application that protects from automated keystroke attacks (such as Bash Bunny or USB Rubber Ducky)

This solution uses a sliding window of time between individual keystrokes. Every time a key is pressed, an average time between keystrokes for the whole sliding window (of size `SLIDING_WINDOW_SIZE`)
is computed and compared to a threshold value (defined in `SLIDING_WINDOW_THRESHOLD` constant). If the average keystroke separation is smaller than the threshold,
it is assumed that automated keystroke attack is being performed. Consequently, for `KEYSTROKES_QUARANTINE_DURATION` seconds, all keystrokes are blocked.
Optionally, the user can also be locked out from his Windows session - which is controlled by `LOCK_SCREEN` constant.

Inspired by (https://stackoverflow.com/questions/43712486/windows-service-keylogger)[Stack Overflow] and (https://github.com/pmsosa/duckhunt)[Duckhunt].