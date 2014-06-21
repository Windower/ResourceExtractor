# Place file in FINAL FANTASY XI directory and invoke with any Python 3 interpreter
import argparse
import os
import errno
import re

parser = argparse.ArgumentParser(desc='Tool to search FFXI DAT files for certain strings')
parser.add_argument('match', nargs='?', default=None)
parser.add_argument('-a', '--all', action='store_const', const=True, default=False)
parser.add_argument('-d', '--decode', action='store_const', const=False, default=True)
parser.add_argument('-c', '--console', action='store_const', const=True, default=False)
parser.add_argument('-v', '--verbose', action='store_const', const=True, default=False)

args = parser.parse_args()

root = os.path.dirname(os.path.realpath(__file__)) + '/'

match_arrays = {}
dmsg = bytearray(ord(c) for c in 'd_msg\0\0\0')
xistr = bytearray(ord(c) for c in 'XISTRING')
b = bytearray([1])
lut = {}

def getdat(path):
    tokens = p.findall(path)
    id = (int(tokens[-2]) << 7) | int(tokens[-1])

    if not lut:
        with open('FTABLE.DAT', 'rb') as ftable:
            b = ftable.read(2)
            while b:
                lut[b[0] | (b[1] << 8)] = (ftable.tell() - 2) / 2
                b = ftable.read(2)

    return lut[id]

def getpath(dat):
    with open('FTABLE.DAT', 'rb') as ftable:
        ftable.seek(dat*2)
        num = ftable.read(1)[0] | (ftable.read(1)[0] << 8)
        return root + 'ROM/%d/%d.DAT' % (num >> 7, num & 0x7F)

def getenc(f):
    format = f.read(4)
    f.seek(0, 2)
    checksum = 0x10000000 + f.tell() - 4
    f.seek(0x04)

    if format[0] | (format[1] << 8) | (format[2] << 16) | (format[3] << 24) == checksum:
        return 0x80

    format += f.read(4)
    if format == xistr or format == dmsg:
        f.seek(0x0A)
        enc = f.read(1) == b
        f.seek(0x40)
        return 0xFF if enc else 0x00

    f.seek(0x08)
    return 0x00

def getmatch(f):
    return match_arrays[getenc(f)]

matches = {}

def decode(index_or_path):
    path = matches[index_or_path] if type(index_or_path) is int else index_or_path
    with open(path, 'rb') as f:
        outpath = root + 'DEC/' + '/'.join(path.split('/')[-3:])
        try:
            os.makedirs(os.path.dirname(outpath))
        except OSError as ex:
            if ex.errno != errno.EEXIST:
                raise

        print('    Saving decoded file to %s' % outpath)

        with open(outpath, 'wb') as out:
            enc = getenc(f)
            pos = f.tell()
            f.seek(0)
            out.write(f.read(pos))
            f.seek(pos)

            out.write(bytearray(c ^ enc for c in f.read()))

        if args.match:
            collection = {}
            collection[args.match] = set(['/'.join(outpath.split('/')[-3:])])

            decfile = root + 'DEC/decoded.txt'
            if os.path.exists(decfile):
                with open(decfile, 'r') as f:
                    title = ''
                    for line in f:
                        if line == '':
                            continue

                        if not line.startswith(' '):
                            title = line[:-2]
                        else:
                            if not title in collection:
                                collection[title] = set([])
                            collection[title].add(line[4:-1])

            with open(decfile, 'w') as f:
                for name in sorted(collection):
                    f.write(name + ':\r\n')
                    for path in collection[name]:
                        f.write('    ' + path + '\r\n')
                    f.write('\r\n')

if args.match:
    match_name = bytearray(args.match.encode('shift-jis'))
    match_arrays[0x00] = match_name
    match_arrays[0xff] = bytearray(c ^ 0xFF for c in match_name)
    match_arrays[0x80] = bytearray(c ^ 0x80 for c in match_name)

    print()
    print('Searching for "%s"' % args.match)
    print('----------------')

    p = re.compile('\d+')

    for name in (x for x in os.listdir() if x.startswith('ROM') or x == '0'):
        if not args.all and name != 'ROM':
            continue

        d = root + name + '/'
        olddir = ''
        print('Searching in %s...' % d)
        for subdir, dirs, files in os.walk(d):
            if args.verbose and subdir != olddir:
                print('Searching in %s...' % subdir)
                olddir = subdir

            for filename in (f for f in files if f.endswith('.DAT')):
                lookup = subdir + '/' + filename
                with open(lookup, 'rb') as f:
                    if getmatch(f) in f.read():
                        print('    Found ID 0x%.4X: %s' % (getdat(lookup), lookup))
                        matches[len(matches) + 1] = lookup
                        if args.decode:
                            decode(lookup)

if args.console:
    import readline
    import code

    vars = globals().copy()
    vars.update(locals())

    shell = code.InteractiveConsole(vars)
    shell.interact()
