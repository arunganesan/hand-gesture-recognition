#! /usr/bin/env python

from twisted.internet import reactor
from autobahn.websocket import WebSocketServerFactory, WebSocketServerProtocol, listenWS
import random 
 
class EchoServerProtocol(WebSocketServerProtocol):
 
   def onMessage(self, msg, binary):
      maxWidth = 700
      maxHeight = 200
      x = random.randint(0, maxWidth)
      y = random.randint(0, maxHeight)
      newMsg= '%d,%d' % (x, y)
      self.sendMessage(newMsg, binary)
      #self.sendMessage(msg, binary)
 
if __name__ == '__main__':
   print "Hello World"
   factory = WebSocketServerFactory("ws://localhost:9000")
   factory.protocol = EchoServerProtocol
   listenWS(factory)
   reactor.run()
