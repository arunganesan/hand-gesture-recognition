import win32api, win32con

def move(x, y): win32api.SetCursorPos((x, y))
def up(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, x, y, 0, 0)
def down(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0)

if __name__ == '__main__':
    # Start a socket connection to the server and start interpreting responses
    move(300, 10);
    down(300, 10);
    up(300, 10);
    down(300, 10);
    up(300, 10);
    