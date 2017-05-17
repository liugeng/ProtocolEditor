# -*- coding: cp936 -*-

#-------------------------------------------------------------------------------

output = r'..\..\BoloProj'

classFileName       = 'Net_Classes.bolos'
dispatcherName      = 'Net_Dispatcher.bolos'
parseFileName       = 'Net_{0}_Parse.bolos'
handleFileName      = 'Net_{0}_Handle.bolos'
senderFileName      = 'Net_{0}_Send.bolos'
debugFileName       = 'Net_{0}_Debug.bolos'

header1     = '/******************************************************************************\n'
header2     = u'* 由ProtocolEditor工具自动生成'.encode('utf-8')
header3     = u', 请勿手动修改'.encode('utf-8')
header4     = '******************************************************************************/\n'

tab         = '    '
iters       = ['i', 'j', 'k', 'l', 'm', 'n']
iterIdx     = 0
tmpPath     = './tmp'
#-------------------------------------------------------------------------------

isIronPy = vars().has_key('jsonFile')

if isIronPy:
    #solve the 'no module named os' problem when excute by IronPython
    import sys
    sys.path.append(r"C:\Python27\Lib")

import os
import json
import filecmp
import re


if not isIronPy:
    print('Test Mode')
    output = './output'
    if not os.path.exists(output):
        os.mkdir(output)
    jsonFile = './ProtocolEditor.Msg.json'

if not vars().has_key('argstr'):
    args = {'gentype':'none'}
else:
    args = json.loads(argstr)
#debug
'''
args = {
    'gentype':'msg_dump',
    'gname':'OpenChest',
    'mname':'BuySoul',
    'mtype':'CS'
}
'''

print(jsonFile)
fp = open(jsonFile, 'r')
cfg = json.loads(fp.read())
fp.close()



#转换type类型
def T(t, needClassPrefix=False):
    ret = t['type']
    if ret == 'byte' or ret == 'bool':
        ret = 'int'
        
    if t['isArray']:
        if t['isClass']:
            return 'class {0}[]'.format(ret)
        return ret + '[]'
    elif t['isClass'] and needClassPrefix:
        return 'class ' + ret
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
    fp.write(u'* 根据协议号分发协议到不同的函数处理\n'.encode('utf-8'))
    fp.write(header4 + '\n')

    for g in cfg['groups']:
        fp.write('import "' + parseFileName.format(g['name']) + '";\n')
    fp.write('\n')

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

            #fp.write(tab + 'call("' + handleFileName.format(g['name'], m['name']) + '", 1, buf);\n')
            fp.write(tab + 'parse_' + g['name'] + '_' + m['name'] + '(buf);\n')
    fp.write('}')
    fp.close()


def getClass(name):
    for c in cfg['classes']:
        if c['name'] == name:
            return c
    return None


def writeVar(fp, vtype, name, isArr, isClass, t, arrLenType):
    global iterIdx
    if isArr:
        it = iters[iterIdx]
        iterIdx += 1
        
        fp.write('{0}buf.{4}(arrayLen({1}));\n'
                 '{0}for (int {2} = 0; {2} < arrayLen({1}); {2}++) {{\n'
                 '{0}    {3} tmp = {1}[{2}];\n'
                 .format(t, name, it, vtype,
                         arrLenType=='int' and 'writeInt' or 'writeShort'))
        writeVar(fp, vtype, "tmp", False, isClass, t+tab, "")
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
            writeVar(fp, v['type'], name+'.'+v['name'], v['isArray'], v['isClass'], t, v['arrLenType'])


def genSendFunc(fp, m, g):
    fp.write('void send_{0}_{1}('.format(g['name'], m['name']))
    firstOne = True
    for v in m['vars']:
        if firstOne:
            firstOne = False
        else:
            fp.write(', ')
        fp.write(T(v, True) + ' ' + v['name'])
        
    fp.write(') {{\n'
             '    BoloArray buf = new BoloArray();\n'
             '    buf.writeShort({0});\n'
             .format(m['id']))

    for v in m['vars']:
        iterIdx = 0
        writeVar(fp, v['type'], v['name'], v['isArray'], v['isClass'], tab, v['arrLenType'])

    if args['gentype'] == 'msg_dump' and args['mname'] == m['name']:
        fp.write('\n')
        fp.write(tab + '//dump begin\n')
        fp.write(tab + 'print("[' + m['name'] + '] CS ' + m['id'] + '");\n')
        for v in m['vars']:
            iterIdx = 0
            dumpVar(fp, v['name'], v['type'], v['isArray'], v['isClass'], tab)
        fp.write(tab + '//dump end\n')

    fp.write('\n'
             '    @GameMain.send(buf);\n'
             '}\n')

#生成发送函数文件
def genSenderFile():
    global iterIdx;
    for g in cfg['groups']:
        gentype = args['gentype']
        if (gentype == 'group' or gentype == 'msg' or gentype == 'msg_dump') and args['gname'] != g['name']:
            continue

        filepath = os.path.join(output, senderFileName.format(g['name']))
        if os.path.exists(filepath) and (gentype == 'msg' or gentype == 'msg_dump'):
            fp = open(filepath, 'r')
            tfp = open(tmpPath, 'w')
            override = False
            for line in fp.readlines():
                r = re.match('void send_'+g['name']+'_(\w+)', line)
                if r and r.groups()[0] == args['mname']:
                    override = True
                    for m in g['msgs']:
                        if m['name'] == r.groups()[0] and m['type'] == 'CS':
                            genSendFunc(tfp, m, g)
                            break
                elif override == False:
                    tfp.write(line)
                elif re.match('^}', line):
                    override = False
                
            fp.close()
            tfp.close()
            if not filecmp.cmp(filepath, tmpPath):
                os.remove(filepath)
                os.rename(tmpPath, filepath)
                    
            if os.path.exists(tmpPath):
                os.remove(tmpPath)

        else:
            fp = open(filepath, 'w')
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
                    
                genSendFunc(fp, m, g)
                
            fp.close()



def readVar(fp, vtype, name, isArr, isClass, t, arrLenType):
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
        if arrLenType == 'int':
            fp.write(t + 'int '+lenVar+' = buf.readInt();\n')
        else:
            fp.write(t + 'short '+lenVar+' = buf.readShort();\n')
        
        if isClass:
            if name.find('.') > 0:
                fp.write('{0}{1} = new class {2}[{3}];\n'.format(t, name, vtype, lenVar))
            else:
                fp.write('{0}class {1}{2} = new class {3}[{4}];\n'.format(t, typeStr, name, vtype, lenVar))
        else:
            fp.write('{0}{1}{2} = new {3}[{4}];\n'.format(t, typeStr, name, vtype, lenVar))
        fp.write(t + 'for (int '+it+' = 0; '+it+' < '+lenVar+'; '+it+'++) {\n')
        readVar(fp, vtype, 'tmp'+it, False, isClass, t+tab, "")
        fp.write(t + tab + name+'['+it+'] = tmp'+it+';\n')
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
            readVar(fp, v['type'], name+'.'+v['name'], v['isArray'], v['isClass'], t, v['arrLenType'])

            
def genHandleFunc(fp, m, g):
    fp.write('void handle_' + g['name'] + '_' + m['name'] + '(')
    firstOne = True
    for v in m['vars']:
        if firstOne:
            firstOne = False
        else:
            fp.write(', ')
        fp.write(T(v, True) + ' ' + v['name'])
    fp.write(') {\n')


lenVarIdx = 1
def dumpVar(fp, name, vtype, isArr, isClass, t):
    global iterIdx
    global lenVarIdx

    rawName = name
    pos = name.rfind('.')
    if pos > 0:
        rawName = name[pos+1:]
    
    if isArr:
        it = iters[iterIdx]
        iterIdx += 1

        lenVar = 'len' + str(lenVarIdx)
        lenVarIdx += 1

        fp.write(('{0}int {1} = arrayLen({2});\n'
                  '{0}print("{0}{4} len: " + {1});\n'
                  '{0}for (int {3} = 0; {3} < {1}; {3}++) {{\n')
                  .format(t, lenVar, name, it, rawName))
        if isClass:
            tmpName = 'tmp'+it.upper()
            fp.write(t + tab + '{0} {1} = {2}[{3}];\n'.format(vtype, tmpName, name, it))
            dumpVar(fp, tmpName, vtype, False, isClass, t + tab)
        else:
            dumpVar(fp, name+'['+it+']', vtype, False, isClass, t + tab)
        fp.write(t + '};\n')
    elif isClass:
        c = getClass(vtype)
        for v in c['vars']:
            dumpVar(fp, name + '.' + v['name'], v['type'], v['isArray'], v['isClass'], t)
    else:
        fp.write('{0}print("{0}{1}: " + {2});\n'.format(t, rawName, name))


def genParseFunc(fp, m, g):
    global iterIdx
    global lenVarIdx

    fp.write('void parse_' + g['name'] + '_' + m['name'] + '(class BoloData buf) {\n')
    if len(m['vars']) > 0:
        for v in m['vars']:
            iterIdx = 0
            readVar(fp, v['type'], v['name'], v['isArray'], v['isClass'], tab, v['arrLenType'])
        fp.write('\n')

    if args['gentype'] == 'msg_dump' and args['mname'] == m['name']:
        lenVarIdx = 1
        fp.write(tab + '//dump begin\n')
        fp.write(tab + 'print("[' + m['name'] + '] SC ' + m['id'] + '");\n')
        for v in m['vars']:
            iterIdx = 0
            dumpVar(fp, v['name'], v['type'], v['isArray'], v['isClass'], tab)
        fp.write(tab + '//dump end\n\n')
   
    fp.write(tab+'handle_' + g['name'] + '_' + m['name'] + '(')
    
    firstOne = True
    for v in m['vars']:
        if firstOne:
            firstOne = False
        else:
            fp.write(', ')
        fp.write(v['name'])
    fp.write(');\n}\n')
    
#生成协议解析文件
def genHandleFile():
    for g in cfg['groups']:
        gentype = args['gentype']
        if (gentype == 'group' or gentype == 'msg' or gentype == 'msg_dump') and args['gname'] != g['name']:
            continue

        filepath = os.path.join(output, handleFileName.format(g['name']))
        if not os.path.exists(filepath):
            fp = open(filepath, 'w')
            fp.write(header1)
            fp.write(header2 + u', 重新生成只会覆盖handle的参数列表'.encode('utf-8') + '\n')
            fp.write('* [{0}]'.format(g['name']))
            if g['comment'] != '':
                fp.write(Comment(g))
            fp.write('\n')
            fp.write(header4 + '\n')

            fp.write('import "{0}";\n\n'.format(classFileName))

            for m in g['msgs']:
                if m['type'] != 'SC':
                    continue

                if m['comment'] != '':
                    fp.write('//' + Comment(m) + '\n')
                genHandleFunc(fp, m, g)
                fp.write(tab + 'print("handle {0}");\n'.format(m['name']))
                fp.write('}\n\n')
                    
            fp.close()
        else:
            fp = open(filepath, 'rb')
            tmpFp = open(tmpPath, 'wb')

            #copy
            msgs = []
            for m in g['msgs']:
                if m['type'] == 'SC':
                    msgs.append(m)
                
            for line in fp.readlines():
                r = re.match('void handle_'+g['name']+'_(\w+)', line)
                if r:
                    mname= r.groups()[0]
                    for m in msgs:
                        if m['name'] == mname:
                            genHandleFunc(tmpFp, m, g)
                            msgs.remove(m)
                            break
                else:
                    tmpFp.write(line)

            if len(msgs) > 0:
                for m in msgs:
                    genHandleFunc(tmpFp, m, g)
                    tmpFp.write(tab + 'print("handle {0}");\n'.format(m['name']))
                    tmpFp.write('}\n\n')
                
            tmpFp.close()
            fp.close()
            if not filecmp.cmp(filepath, tmpPath):
                os.remove(filepath)
                os.rename(tmpPath, filepath)
                    
        if os.path.exists(tmpPath):
            os.remove(tmpPath)


def genParseFile():
    for g in cfg['groups']:
        gentype = args['gentype']
        if (gentype == 'group' or gentype == 'msg' or gentype == 'msg_dump') and args['gname'] != g['name']:
            continue

        filepath = os.path.join(output, parseFileName.format(g['name']))

        if os.path.exists(filepath) and (gentype == 'msg' or gentype == 'msg_dump'):
            fp = open(filepath, 'r')
            tfp = open(tmpPath, 'w')
            override = False
            for line in fp.readlines():
                r = re.match('void parse_'+g['name']+'_(\w+)', line)
                if r and r.groups()[0] == args['mname']:
                    override = True
                    for m in g['msgs']:
                        if m['name'] == r.groups()[0] and m['type'] == 'SC':
                            genParseFunc(tfp, m, g)
                            break
                elif override == False:
                    tfp.write(line)
                elif re.match('^}', line):
                    override = False
            fp.close()
            tfp.close()
            if not filecmp.cmp(filepath, tmpPath):
                os.remove(filepath)
                os.rename(tmpPath, filepath)
                    
            if os.path.exists(tmpPath):
                os.remove(tmpPath)
            
        else:
            fp = open(filepath, 'w')
            fp.write(header1)
            fp.write(header2 + header3 + '\n')
            fp.write('* [{0}]'.format(g['name']))
            if g['comment'] != '':
                fp.write(Comment(g))
            fp.write('\n')
            fp.write(header4 + '\n')

            fp.write('import "' + handleFileName.format(g['name']) + '";\n')
                
            for m in g['msgs']:
                if m['type'] != 'SC':
                    continue

                fp.write('\n')
                if m['comment'] != '':
                    fp.write('//' + Comment(m) + '\n')
                genParseFunc(fp, m, g)
                
            fp.close()


def setDefaultVar(fp, vtype, name, isArr, isClass, t):
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
        fp.write(t + 'int '+lenVar+' = 1;\n')
        
        if isClass:
            if name.find('.') > 0:
                fp.write('{0}{1} = new class {2}[{3}];\n'.format(t, name, vtype, lenVar))
            else:
                fp.write('{0}class {1}{2} = new class {3}[{4}];\n'.format(t, typeStr, name, vtype, lenVar))
        else:
            fp.write('{0}{1}{2} = new {3}[{4}];\n'.format(t, typeStr, name, vtype, lenVar))
        fp.write(t + 'for (int '+it+' = 0; '+it+' < '+lenVar+'; '+it+'++) {\n')
        setDefaultVar(fp, vtype, 'tmp'+it, False, isClass, t+tab)
        fp.write(t + tab + name+'['+it+'] = tmp'+it+';\n')
        fp.write(t + '}\n')
    elif vtype == 'int' or vtype == 'short' or vtype == 'long' or vtype == 'byte' or vtype == 'float' or vtype == 'bool':
        fp.write('{0}{1}{2} = 0;\n'.format(t, typeStr, name))
    elif vtype == 'String':
        fp.write('{0}{1}{2} = "";\n'.format(t, typeStr, name))
    elif isClass:
        c = getClass(vtype)
        fp.write('{0}{1}{2} = new {3}();\n'.format(t, typeStr, name, vtype))
        for v in c['vars']:
            setDefaultVar(fp, v['type'], name+'.'+v['name'], v['isArray'], v['isClass'], t)

def genDebugFile():
    global iterIdx
    for g in cfg['groups']:
        if args['gname'] != g['name']:
            continue
        
        fp = open(os.path.join(output, debugFileName.format(g['name'])), 'w')
        fp.write(header1)
        fp.write(header2 + u'，仅用于测试'.encode('utf-8') + '\n')
        fp.write('* [{0}]'.format(g['name']))
        if g['comment'] != '':
            fp.write(Comment(g))
        fp.write('\n')
        fp.write(header4)

        fp.write('\n')
        fp.write('import "' + handleFileName.format(g['name']) + '";\n')

        for m in g['msgs']:
            if m['type'] != 'SC':
                continue
            fp.write('\n')

            if m['comment'] != '':
                fp.write('//' + Comment(m) + '\n')
                
            fp.write('void debugSend_{0}_{1}() {{\n'.format(g['name'], m['name']))

            if len(m['vars']) > 0:
                for v in m['vars']:
                    iterIdx = 0
                    setDefaultVar(fp, v['type'], v['name'], v['isArray'], v['isClass'], tab)
                fp.write('\n')
           
            fp.write(tab+'handle_' + g['name'] + '_' + m['name'] + '(')
            
            firstOne = True
            for v in m['vars']:
                if firstOne:
                    firstOne = False
                else:
                    fp.write(', ')
                fp.write(v['name'])
            fp.write(');\n}\n')
        fp.close()
        


#----------------------------------------------

gentype = args['gentype']

if gentype == 'debug':
    genDebugFile()

elif gentype == 'msg' or gentype == 'msg_dump':
    genClassFile()
    genDispatcherFile()
    if args['mtype'] == 'CS':
        genSenderFile()
    else:
        genHandleFile()
        genParseFile()

else:
    genClassFile()
    genDispatcherFile()
    genSenderFile()
    genHandleFile()
    genParseFile()

    
