#! /usr/bin/env python

import win32api, win32con

def move(x, y): win32api.SetCursorPos((x, y))
def up(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, x, y, 0, 0)
def down(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0)
def wheelup(): win32api.mouse_event(win32con.MOUSEEVENTF_WHEEL, 0, 0, 20, 0)

if __name__ == '__main__':
    # Start a socket connection to the server and start interpreting responses
    x, y = (150, 150)
    move(x, y);
    down(x, y);
    up(x, y);
    wheelup();
    wheelup();