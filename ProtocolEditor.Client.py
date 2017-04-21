# -*- coding: cp936 -*-

#-----------------------------------------------------------
output = './output'

classFileName       = 'Net_Classes.bolos'
dispatcherName      = 'Net_Dispatcher.bolos'
handleFileName      = 'Net_Handle_{0}_{1}'
senderFileName      = 'Net_Send_{0}.bolos'

header1     = '/******************************************************************************\n'
header2     = u'* 由ProtocolEditor工具自动生成'.encode('utf-8')
header3     = u', 请勿手动修改'.encode('utf-8')
header4     = '******************************************************************************/\n'

tab         = '    '
iters       = ['i', 'j', 'k', 'l', 'm', 'n']
iterIdx     = 0
tmpPath     = './tmp'
#-----------------------------------------------------------

isIronPy = vars().has_key('jsonFile')

if isIronPy:
    #solve the 'no module named os' problem when excute by IronPython
    import sys
    sys.path.append(r"C:\Python27\Lib")

import os
import json
import filecmp


if not isIronPy:
    print('Test Mode')
    output = './output'
    if not os.path.exists(output):
        os.mkdir(output)
    jsonFile = './ProtocolEditor.Msg.json'



fp = open(jsonFile, 'r')
cfg = json.loads(fp.read())
fp.close()



#转换type类型
def T(t):
    ret = t['type']
    if ret == 'byte' or ret == 'bool':
        ret = 'int'
        
    if t['isArray']:
        if t['isClass']:
            return 'class {0}[]'.format(ret)
        return ret + '[]'
    return ret


def Comment(t):
    if t['comment'] == '':
        return ''
    if not isIronPy:
        return t['comment'].encode('utf-8')
    return t['comment']
    

#生成数据类文件
def genClassFile():
    fp = open(os.path.join(output, classFileName), 'w')
    fp.write(header1)
    fp.write(header2 + header3 + '\n')
    fp.write(header4)

    for c in cfg['classes']:
        fp.write('\n')
        if c['comment'] != '':
            fp.write('//' + Comment(c) + '\n')

        fp.write('class {0} {{\n'.format(c['name']))
        for v in c['vars']:
            s = '    {0} {1};'.format(T(v), v['name'])
            fp.write(s)
            if v['comment'] != '':
                fp.write('\t'.expandtabs(40-len(s)) + '//' + Comment(v))
            fp.write('\n')

        fp.write('\n')
        fp.write(tab + 'void descript() {\n')
        fp.write(tab + tab + 'print("{0}");\n'.format(c['name']))
        for v in c['vars']:
            if v['isArray']:
                fp.write(tab + tab + 'print("  {0}: array len " + arrayLen({0}));\n'.format(v['name']))
            elif v['isClass']:
                fp.write(tab + tab + 'print("  {0}: <class {1}>");\n'.format(v['name'], v['type']))
            else:
                fp.write(tab + tab + 'print("  {0}: " + {0});\n'.format(v['name']))
        fp.write(tab + '}\n')
        fp.write('};\n')
    fp.close()


#生成协议分发文件
def genDispatcherFile():
    fp = open(os.path.join(output, dispatcherName), 'w')
    fp.write(header1)
    fp.write(header2 + header3 + '\n')
    fp.write(u'* 根据协议号分发协议到不同的文件处理\n'.encode('utf-8'))
    fp.write(header4 + '\n')

    fp.write('BoloData buf = load();\n'
             'int cmd = buf.readShort();\n')
    firstOne = True
    for g in cfg['groups']:
        for m in g['msgs']:
            if m['type'] != 'SC':
                continue
            if firstOne:
                firstOne = False
                fp.write('if (cmd == {0}) {{\n'.format(m['id']))
            else:
                fp.write('}} else if (cmd == {0}) {{\n'.format(m['id']))

            fp.write(tab + 'call("' + handleFileName.format(g['name'], m['name']) + '", 1, buf);\n')
    fp.write('}')
    fp.close()


def getClass(name):
    for c in cfg['classes']:
        if c['name'] == name:
            return c
    return None


def writeVar(fp, vtype, name, isArr, isClass, t):
    global iterIdx
    if isArr:
        it = iters[iterIdx]
        iterIdx += 1
        fp.write('{0}buf.writeInt(arrayLen({1}));\n'
                 '{0}for (int {2} = 0; {2} < arrayLen({1}); {2}++) {{\n'
                 '{0}    {3} tmp = {1}[{2}];\n'
                 .format(t, name, it, vtype))
        writeVar(fp, vtype, "tmp", False, isClass, t+tab)
        fp.write(t + '}\n')
    elif vtype == 'int':
        fp.write(t + 'buf.writeInt(' + name + ');\n')
    elif vtype == 'float':
        fp.write(t + 'buf.writeFloat(' + name + ');\n')
    elif vtype == 'String':
        fp.write(t + 'buf.writeUTF(' + name + ');\n')
    elif vtype == 'short':
        fp.write(t + 'buf.writeShort(' + name + ');\n')
    elif vtype == 'byte':
        fp.write(t + 'buf.writeByte(' + name + ');\n')
    elif vtype == 'long':
        fp.write(t + 'buf.writeLong(' + name + ');\n')
    elif vtype == 'bool':
        fp.write(t + 'buf.writeByte(' + name + ');\n')
    elif isClass:
        c = getClass(vtype)
        for v in c['vars']:
            writeVar(fp, v['type'], name+'.'+v['name'], v['isArray'], v['isClass'], t)


#生成发送函数文件
def genSenderFile():
    for g in cfg['groups']:
        fp = open(os.path.join(output, senderFileName.format(g['name'])), 'w')
        fp.write(header1)
        fp.write(header2 + header3 + '\n')
        fp.write('* [{0}]'.format(g['name']))
        if g['comment'] != '':
            fp.write(Comment(g))
        fp.write('\n')
        fp.write(header4)

        for m in g['msgs']:
            if m['type'] != 'CS':
                continue
            fp.write('\n')

            if m['comment'] != '':
                fp.write('//' + Comment(m) + '\n')
                
            fp.write('void Send_{0}_{1}('.format(g['name'], m['name']))
            firstOne = True
            for v in m['vars']:
                if firstOne:
                    firstOne = False
                else:
                    fp.write(', ')
                fp.write(T(v) + ' ' + v['name'])
                
            fp.write(') {{\n'
                     '    BoloArray buf = new BoloArray();\n'
                     '    buf.writeShort({0});\n'
                     .format(m['id']))

            for v in m['vars']:
                writeVar(fp, v['type'], v['name'], v['isArray'], v['isClass'], tab)
                
            fp.write('\n'
                     '    @GameMain.send(buf);\n'
                     '}\n')
        fp.close()



def readVar(fp, vtype, name, isArr, isClass, t):
    global iterIdx
    typeStr = ''
    if name.find('.') < 0:
        typeStr = vtype + ' '
        if vtype == 'byte':
            typeStr = 'int '
        
    if isArr:
        it = iters[iterIdx]
        iterIdx += 1
        if typeStr != '':
            typeStr = vtype + '[] '

        pos = name.find('.')
        lenVar = name[pos+1:] + 'Len' + it.upper()
        fp.write(t + 'int '+lenVar+' = buf.readInt();\n')
        
        if isClass:
            if name.find('.') > 0:
                fp.write('{0}{1} = new class {2}[{3}];\n'.format(t, name, vtype, lenVar))
            else:
                fp.write('{0}class {1}{2} = new class {3}[{4}];\n'.format(t, typeStr, name, vtype, lenVar))
        else:
            fp.write('{0}{1}{2} = new {3}[{4}];\n'.format(t, typeStr, name, vtype, lenVar))
        fp.write(t + 'for (int '+it+' = 0; '+it+' < '+lenVar+'; '+it+'++) {\n')
        readVar(fp, vtype, 'tmp'+it, False, isClass, t+tab)
        fp.write(t + tab + name+'['+it+'] = obj'+it+';\n')
        fp.write(t + '}\n')
    elif vtype == 'int':
        fp.write('{0}{1}{2} = buf.readInt();\n'.format(t, typeStr, name))
    elif vtype == 'float':
        fp.write('{0}{1}{2} = buf.readFloat();\n'.format(t, typeStr, name))
    elif vtype == 'String':
        fp.write('{0}{1}{2} = buf.readUTF();\n'.format(t, typeStr, name))
    elif vtype == 'short':
        fp.write('{0}{1}{2} = buf.readShort();\n'.format(t, typeStr, name))
    elif vtype == 'byte':
        fp.write('{0}{1}{2} = buf.readByte();\n'.format(t, typeStr, name))
    elif vtype == 'long':
        fp.write('{0}{1}{2} = buf.readLong();\n'.format(t, typeStr, name))
    elif vtype == 'bool':
        fp.write('{0}{1}{2} = buf.readByte();\n'.format(t, typeStr, name))
    elif isClass:
        c = getClass(vtype)
        fp.write('{0}{1}{2} = new {3}();\n'.format(t, typeStr, name, vtype))
        for v in c['vars']:
            readVar(fp, v['type'], name+'.'+v['name'], v['isArray'], v['isClass'], t)

            
def genHandleFunc(fp, m):
    fp.write('void handle(')
    firstOne = True
    for v in m['vars']:
        if firstOne:
            firstOne = False
        else:
            fp.write(', ')
        fp.write(T(v) + ' ' + v['name'])
    fp.write(') {\n')

def genParseFunc(fp, m):
    global iterIdx
    fp.write('void parse() {\n')
    if len(m['vars']) > 0:
        fp.write(tab+'BoloData buf = load();\n')
        for v in m['vars']:
            iterIdx = 0
            readVar(fp, v['type'], v['name'], v['isArray'], v['isClass'], tab)
        fp.write('\n')
    fp.write(tab+'handle(')
    
    firstOne = True
    for v in m['vars']:
        if firstOne:
            firstOne = False
        else:
            fp.write(', ')
        fp.write(v['name'])
    fp.write(');\n}\n\nparse();')
    
#生成协议解析文件
def genHandleFile():
    for g in cfg['groups']:
        for m in g['msgs']:
            if m['type'] != 'SC':
                continue
            filepath = os.path.join(output, handleFileName.format(g['name'], m['name']) + '.bolos')
            if not os.path.exists(filepath):
                fp = open(filepath, 'w')
                fp.write(header1)
                fp.write(header2 + u', 重新生成只会覆盖handle的参数和parse函数体'.encode('utf-8') + '\n')
                fp.write('* [{0}]'.format(m['name']))
                if m['comment'] != '':
                    fp.write(Comment(m))
                fp.write('\n')
                fp.write(header4 + '\n')

                fp.write('import "{0}";\n\n'.format(classFileName))
                genHandleFunc(fp, m)
                fp.write(tab + 'print("handle: {0}");\n'.format(m['name']))
                fp.write('}\n\n')
                genParseFunc(fp, m)
                
                fp.close()
            else:
                fp = open(filepath, 'rb')
                tmpFp = open(tmpPath, 'wb')
                for line in fp.readlines():
                    if line.find('void handle(') >= 0:
                        genHandleFunc(tmpFp, m)
                    elif line.find('void parse(') >= 0:
                        genParseFunc(tmpFp, m)
                        break
                    else:
                        tmpFp.write(line)
                tmpFp.close()
                fp.close()
                if not filecmp.cmp(filepath, tmpPath):
                    os.remove(filepath)
                    os.rename(tmpPath, filepath)
        if os.path.exists(tmpPath):
            os.remove(tmpPath)

genClassFile()
genDispatcherFile()
genSenderFile()
genHandleFile()
