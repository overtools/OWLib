import struct

def readString(file):
    lb1 = file.read(1)[0]
    lb2 = 0

    if lb1 > 128:
        lb2 = file.read(1)[0]

    l = (lb1 % 128) + (lb2 * 128)

    return struct.unpack('p' * l, file.read(l))[0]

fmtSz = {
  'c': 1,
  'b': 1,
  '?': 1,
  'h': 2,
  'i': 4,
  'l': 4,
  'q': 8,
  'f': 4,
  'd': 8,
  's': 1,
  'p': 1
}

def read(file, fmt):
    size = 0
    for char in fmt:
        if char == '<': continue
        if char.lower() in fmtSz:
            size += fmtSz[char]
        else:
            print('unrecognized fmt char %s' % (char))
    return struct.unpack(fmt, file.read(size))
