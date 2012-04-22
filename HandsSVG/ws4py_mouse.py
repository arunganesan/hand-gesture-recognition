import re
import win32api, win32con
from ws4py.client.threadedclient import WebSocketClient

def move(x, y): win32api.SetCursorPos((x, y))
def up(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, x, y, 0, 0)
def down(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0)
def wheel(d): win32api.mouse_event(win32con.MOUSEEVENTF_WHEEL, 0, 0, d, 0)

class MouseClient(WebSocketClient):
    def opened(self):
        self.scaleX = 10;
        self.scaleY = 5;
        self.state = "OpenHand"
        print "Opened"
    
    def closed(self, code, reason):
        print "Closed down", code, reason
    
    def received_message(self, m):
        print "=> %d %s" % (len(m), str(m))
        m = re.match(r'\((.*),(.*),(.*),(.*)\)', str(m))

        gesture = m.group(1)
        x = int(m.group(2))
        y = int(m.group(3))
        depth = int(m.group(4))
        
        scaledX = x*self.scaleX;
        scaledY = y*self.scaleY;
        
        if (self.state != gesture):
            self.state = gesture;
            if (gesture == "OpenHand"): up(scaledX, scaledY)
            else: down(scaledX, scaledY)

        move(x*self.scaleX, y*self.scaleY)
        
        if (depth < 800): wheel(20)
        elif (depth > 1500): wheel(-20)

if __name__ == '__main__':
    try:
        ws = MouseClient('ws://localhost:8181/', protocols=['sample'])
        ws.daemon = False
        ws.connect()
    except KeyboardInterrupt:
        ws.close()
