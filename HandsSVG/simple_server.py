#!/usr/bin/env python

import socket, threading, time, hashlib, base64

# Brought to you by:
# http://tools.ietf.org/html/rfc6455#section-5.2

def handle(s):
  magic_string = '258EAFA5-E914-47DA-95CA-C5AB0DC85B11'
  
  handshake = {}
  request = s.recv(4096)
  print request
  parts = request.split('\n')
  
  for part in parts:
    if ': ' in part:
      key, val = part.strip().split(': ')
      handshake[key] = val

  secret_key = handshake['Sec-WebSocket-Key']
  secret_hash = hashlib.sha1(secret_key + magic_string).digest()
  secret_encoded = base64.b64encode(secret_hash)

  response = (('''
HTTP/1.1 101 Web Socket Protocol Handshake\r
Upgrade: WebSocket\r
Connection: Upgrade\r
Sec-WebSocket-Accept: %s\r
Sec-WebSocket-Protocol: sample
  ''' % secret_encoded).strip() + '\r\n\r\n')
  
  print response
  s.send(response)
  
  time.sleep(1)
  msg = "(OpenHand,50,50,1000)"
  response = "\x81"
  response += chr(len(msg))
  response += msg
  
  s.send(response)
  s.close()

s = socket.socket()
s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
s.bind(('', 9876));
s.listen(1);
while 1:
  t,_ = s.accept();
  threading.Thread(target = handle, args = (t,)).start()
