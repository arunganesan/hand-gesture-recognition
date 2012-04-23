#! /usr/bin/env python
import sys
from twisted.internet import reactor
from autobahn.websocket import WebSocketClientFactory, \
                               WebSocketClientProtocol, \
                               connectWS


class EchoClientProtocol(WebSocketClientProtocol):

   def onOpen(self):
      print "Open"

   def onMessage(self, msg, binary):
      print "Got echo: " + msg
      

if __name__ == '__main__':
   print "Okay..."
   
   if len(sys.argv) < 2:
      print "Need the WebSocket server address, i.e. ws://localhost:9000"
      sys.exit(1)

   print "Setting up server"
   factory = WebSocketClientFactory(sys.argv[1])
   factory.protocol = EchoClientProtocol
   connectWS(factory)

   print "Running"
   reactor.run()
