import re
import win32api, win32con
from ws4py.client.threadedclient import WebSocketClient

def move(x, y): win32api.SetCursorPos((x, y))
def up(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, x, y, 0, 0)
def down(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0)

class MouseClient(WebSocketClient):
    def opened(self):
        print "Opened"
    
    def closed(self, code, reason):
        print "Closed down", code, reason
    
    def received_message(self, m):
        print "=> %d %s" % (len(m), str(m))
        m = re.match(r'\(.*,(.*),(.*),(.*)\)', str(m))

        scaleX = 10;
        scaleY = 5;

        x = int(m.group(1))
        y = int(m.group(2))
        depth = int(m.group(3))
        
        move(x*scaleX, y*scaleY)
        
        if len(str(m)) == 175:
            self.close(reason='Bye bye')

if __name__ == '__main__':
    try:
        ws = MouseClient('ws://lessentropy.net:9876/', protocols=['sample'])
        ws.daemon = False
        ws.connect()
    except KeyboardInterrupt:
        ws.close()
