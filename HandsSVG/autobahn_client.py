#! /usr/bin/env python
import sys, re
import win32api, win32con
from twisted.internet import reactor
from autobahn.websocket import WebSocketClientFactory, \
                               WebSocketClientProtocol, \
                               connectWS

def move(x, y): win32api.SetCursorPos((x, y))
def up(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, x, y, 0, 0)
def down(x, y): win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0)
def wheel(d): win32api.mouse_event(win32con.MOUSEEVENTF_WHEEL, 0, 0, d, 0)

class EchoClientProtocol(WebSocketClientProtocol):

   def onOpen(self):
      print "Open"
      self.scaleX = 1920.0/640.0;
      self.scaleY = 1080.0/480.0;
      self.prev_depth = -1;
      self.gesture_state = "OpenHand"
      sys.stdout.flush()

   def onMessage(self, msg, binary):
      print "=> " + msg
      sys.stdout.flush();
      
      m = re.match(r'\((.*),(.*),(.*),(.*)\)', str(msg))
        
      gesture = m.group(1)
      x = int(m.group(2))
      y = int(m.group(3))
      depth = int(m.group(4))
      
      if self.prev_depth == -1: self.prev_depth = depth

      scaledX = int(x*self.scaleX);
      scaledY = int(y*self.scaleY);
      
      print gesture, scaledX, scaledY, depth
      sys.stdout.flush()
      move (scaledX, scaledY)
      
      depth_delta = depth - self.prev_depth
      
      if (depth_delta < -15): 
         print "Wheel up"
         wheel(4)
      elif (depth_delta > 15): 
        print "Wheel down"
        wheel(-4) 
      
      self.prev_depth = depth

      if (self.gesture_state != gesture):
         self.gesture_state = gesture;
         if (gesture == "OpenHand"): up(scaledX, scaledY)
         else: down(scaledX, scaledY)
      
      return 
      
      move(x*self.scaleX, y*self.scaleY)
        
if __name__ == '__main__':

   if len(sys.argv) < 2:
      print "Need the WebSocket server address, i.e. ws://localhost:9000"
      sys.exit(1)

   print "Setting up server"
   factory = WebSocketClientFactory(sys.argv[1])
   factory.protocol = EchoClientProtocol
   connectWS(factory)

   print "Running"
   reactor.run()
