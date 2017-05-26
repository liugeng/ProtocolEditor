# -*- coding: cp936 -*-

#-------------------------------------------------------------------------------
#setting

output = r'..\..\client\dev\KhanMainSS2'

dispatcher      = 'NetDispatcher'
parser          = 'NetParser'
handler         = 'NetHandler'
sender          = 'NetSender'
classes         = 'NetClass'


tab         = '    '
iters       = ['i', 'j', 'k', 'l', 'm', 'n']
iterIdx     = 0
tmpPath     = './tmp'
fileCoding  = 'gb2312'

CODE_TYPE_CPP = 1

#-------------------------------------------------------------------------------
#init

isIronPy = vars().has_key('jsonFile')

if isIronPy:
    #solve the 'no module named os' problem when excute by IronPython
    import sys
    sys.path.append(r"C:\Python27\Lib")

import os
import json
import filecmp
import re
import codecs

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


print(jsonFile)

if not isIronPy:
    fp = codecs.open(jsonFile, 'r', fileCoding)
else:
    fp = open(jsonFile, 'r')
cfg = json.loads(fp.read())
fp.close()


#-------------------------------------------------------------------------------
#common func

def T(t, checkIsArr=True):
    s = ''
    vtype = t['type']
    if vtype == 'bool':
        s = 'b2'
    elif vtype == 'byte':
        s = 'u8'
    elif vtype == 'short':
        s = 's16'
    elif vtype == 'int':
        s = 's32'
    elif vtype == 'long':
        s = 's64'
    elif vtype == 'float':
        s = 'f32'
    elif vtype == 'String':
        s = 'string'
    else:
        s = vtype + '*'

    if checkIsArr:
        if t['isArray']:
            return 'ArrayList<' + s + '>*'
    return s

def genVarList(m):
    s = ''
    i = 0
    n = len(m['vars'])
    for v in m['vars']:
        s += '{0} {1}'.format(T(v), v['name'])
        i += 1
        if i < n:
            s += ', '
    return s

def genVarNameList(m):
    s = ''
    i = 0
    n = len(m['vars'])
    for v in m['vars']:
        s += v['name']
        i += 1
        if i < n:
            s += ', '
    return s

def Comment(t):
    if t['comment'] == '':
        return ''
    if not isIronPy:
        return t['comment'].encode(fileCoding)
    return t['comment']

def genMsgComment(fp, m, t):
    line = 0
    if m['comment'] != '':
        line = 1
        fp.write(t + '/* ' + Comment(m))

    maxlen = 0
    for v in m['vars']:
        if v['comment'] != '' and len(v['name']) > maxlen:
            maxlen = len(v['name'])
    
    for v in m['vars']:
        if v['comment'] != '':
            if line == 0:
                s = t + '/* ' + v['name']
            else:
                fp.write('\n')
                s = t + ' * ' + v['name']
            fp.write(s + '\t'.expandtabs(maxlen + len(t+' * ') + 4 - len(s)))
            fp.write(Comment(v))
            line += 1
    if line > 0:
        if line > 1:
            fp.write('\n')
            fp.write(t)
        fp.write(' */\n')
        
def hasClass(m):
    for v in m['vars']:
        if v['isClass']:
            return True
    return False

def genCleanCode(m, t):
    s = ''
    for v in m['vars']:
        if v['isArray']:
            if v['isClass']:
                s += (t + 'for (auto e : {0}) {{\n' +
                      t + '    delete e;\n' +
                      t + '}}\n').format(v['name'])
            s += t + 'delete ' + v['name'] + ';\n'
        elif v['isClass']:
            s += t + 'delete ' + v['name'] + ';\n'
    return s

def _getClass(cname):
    for c in cfg['classes']:
        if c['name'] == cname:
            return c

def _checkClass(c, l):
    if not c['name'] in l:
        l.append(c['name'])
    for v in c['vars']:
        if v['isClass']:
            c = _getClass(v['type'])
            _checkClass(c, l)
    
def getCppClassNames():
    l = []
    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        for m in g['msgs']:
            if m['type'] == 'CS':
                continue
            for v in m['vars']:
                if v['isClass']:
                    c = _getClass(v['type'])
                    _checkClass(c, l)
    return l

def genVarDeclare(fp, c):
    maxlen = 0
    for v in c['vars']:
        if v['comment'] != '':
            s = '    {0} {1};'.format(T(v), v['name'])
            if len(s) > maxlen:
                maxlen = len(s)
    
    for v in c['vars']:
        s = '    {0} {1};'.format(T(v), v['name'])
        fp.write(s)
        if v['comment'] != '':
            fp.write('\t'.expandtabs(maxlen + 4 - len(s)))
            fp.write('//' + Comment(v))
        fp.write('\n')

def genClassConstruct(c):
    s = ''
    first = True
    for v in c['vars']:
        if first:
            first = False
            s += ': '
        else:
            s += ', '

        if v['type'] == 'str':
            s += v['name'] + '("")\n'
        else:
            s += v['name'] + '(0)\n'
    return s

def genClassDestruct(c):
    s = ''
    for v in c['vars']:
        if v['isArray']:
            if v['isClass']:
                s += (tab + 'for (auto e : {0}) {{\n' +
                      tab + '    delete e;\n' +
                      tab + '}}\n'
                      ).format(v['name'])
            s += tab + 'delete {0};\n'.format(v['name'])
        elif v['isClass']:
            s += tab + 'delete {0};\n'.format(v['name'])
    return s

#-------------------------------------------------------------------------------
#gen code : dispatcher

def genDispatchH():
    fp = open(os.path.join(output, dispatcher + '.h'), 'w')
    fp.write('#ifndef _{0}_H_\n'
             '#define _{0}_H_\n'
             '\n'
             '#include "NetHandlerBase.h"\n'
             '\n'
             'using namespace ssf2;\n'
             '\n'
             'class {1};\n'
             'class {2} : public NetHandlerBase {{\n'
             'public:\n'
             '    {2}();\n'
             '    virtual ~{2}();\n'
             'private:\n'
             '    void parseCmd(u16 cmdId, c8* data, u32 len) override;\n'
             '\n'
             '    {1}* parser;\n'
             '}};\n'
             '\n'
             '#endif /* _{0}_H_ */'
             .format(
                 dispatcher.upper(),
                 parser,
                 dispatcher,))
    fp.close()

def genDispatchCode():
    s = ''
    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        for m in g['msgs']:
            if m['type'] == 'CS':
                continue
            
            s += (''
            '        case {0}:\n'
            '            parser->parser_{1}_{2}(buf);\n'
            '            break;\n'
            ).format(
                m['id'],
                g['name'],
                m['name']
                )
    return s

#生成分发
def genDispatchCpp():
    fp = open(os.path.join(output, dispatcher + '.cpp'), 'w')
    fp.write('#include "{0}.h"\n'
             '#include "{1}.h"\n'
             '\n'
             '{0}::{0}() : parser(new {1}()) {{\n'
             '}}\n'
             '\n'
             '{0}::~{0}() {{\n'
             '    delete parser;\n'
             '}}\n'
             '\n'
             'void {0}::parseCmd(u16 cmd, c8* data, u32 len) {{\n'
             '    iobuf buf(data, len);\n'
             '    buf.skip(2);\n'
             '    switch (cmd) {{\n'
             '{2}'
             '        default:\n'
             '            break;\n'
             '    }}\n'
             '}}\n'
             .format(
                 dispatcher,
                 parser,
                 genDispatchCode()
                 ))
    fp.close()

#-------------------------------------------------------------------------------
#gen code: handler

def genHandlerH():
    fp = open(os.path.join(output, handler + '.h'), 'w')
    fp.write('#ifndef _{0}_H_\n'
             '#define _{0}_H_\n'
             '\n'
             '#include "{2}.h"\n'
             '\n'
             'class {1} {{\n'
             'public:\n'
             .format(
                 handler.upper(),
                 handler,
                 classes
                 ))

    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        for m in g['msgs']:
            if m['type'] == 'CS':
                continue
            fp.write('    bool handler_{0}_{1}({2});\n'.format(g['name'], m['name'], genVarList(m)))
            

    fp.write('}};\n'
             '\n'
             '#endif /* _{0}_H_ */'.format(handler.upper()))
    
    fp.close()


def editHandlerCpp(filepath, g):
    fp = open(filepath, 'r')
    tfp = open(tmpPath, 'w')

    msgs = []
    for m in g['msgs']:
        msgs.append(m)

    for line in fp.readlines():
        regex = 'bool ' + handler + '::handler_[^_]+_(\w+)'
        r = re.match(regex, line)
        if r:
            m = None
            for m1 in msgs:
                if m1['name'] == r.groups()[0] and m1['type'] == 'SC':
                    m = m1
                    msgs.remove(m1)
                    break
            if m:
                tfp.write('bool {0}::handler_{1}_{2}({3}) {{\n'
                          .format(
                              handler,
                              g['name'],
                              m['name'],
                              genVarList(m)
                              ))
            else:
                tfp.write('//"' + r.groups()[0] + '" is missing\n')
                tfp.write(line)
        else:
            if not re.match('//"\w+" is missing', line):
                tfp.write(line)

    if len(msgs) > 0:
        for m in msgs:
            if m['type'] == 'CS':
                continue
            tfp.write('\n')
            genMsgComment(tfp, m, '')
            tfp.write('bool {0}::handler_{1}_{2}({3}) {{\n'
                     .format(
                         handler,
                         g['name'],
                         m['name'],
                         genVarList(m)
                         ))
            tfp.write('    return true;\n')
            tfp.write('}\n')
    
    fp.close()
    tfp.close()

    if not filecmp.cmp(filepath, tmpPath):
        os.remove(filepath)
        os.rename(tmpPath, filepath)
    else:
        os.remove(tmpPath)
    

def genHandlerCpp():
    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        filepath = os.path.join(output, handler + g['name'] + '.cpp')
        if os.path.exists(filepath):
            editHandlerCpp(filepath, g)
        else:
            fp = open(filepath, 'w')
            fp.write('#include "' + handler + '.h"\n')

            for m in g['msgs']:
                if m['type'] == 'CS':
                    continue
                fp.write('\n')
                genMsgComment(fp, m, '')
                fp.write('bool {0}::handler_{1}_{2}({3}) {{\n'
                         .format(
                             handler,
                             g['name'],
                             m['name'],
                             genVarList(m)
                             ))
                fp.write('    return true;\n')
                fp.write('}\n')
            fp.close()

#-------------------------------------------------------------------------------
#gen code: parser

def genParserH():
    fp = open(os.path.join(output, parser + '.h'), 'w')
    fp.write('#ifndef _{0}_H_\n'
             '#define _{0}_H_\n'
             '\n'
             '#include <gstl.h>\n'
             '\n'
             'class {1};\n'
             'class {2} {{\n'
             'public:\n'
             '    {2}();\n'
             '    virtual ~{2}();\n'
             .format(
                 parser.upper(),
                 handler,
                 parser
                 ))

    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        for m in g['msgs']:
            if m['type'] == 'CS':
                continue
            fp.write('    void parser_{0}_{1}(iobuf& buf);\n'.format(g['name'], m['name']))

    fp.write('private:\n'
             '    {0}* handler;\n'
             '    s32 _length;\n'
             '}};\n'
             '\n'
             '#endif /* _{1}_H_ */'
             .format(
                 handler,
                 handler.upper()
                 ))
    
    fp.close()

def genReaderVar(fp, v, vtype, name, isArr, isClass, arrLenType, t):
    global iterIdx
    
    typestr = ''
    if name.find('->') < 0:
        typestr = T(v, False) + ' '
        
    if isArr:
        it = iters[iterIdx]
        iterIdx += 1
        if typestr != '':
            typestr = T(v) + ' '

        pos = name.find('.')
        if arrLenType == 'int':
            fp.write(t + '_length = buf.readInt32();\n')
        else:
            fp.write(t + '_length = buf.readInt16();\n')
        
        if isClass:
            if name.find('.') > 0:
                fp.write('{0}{1} = new ArrayList<{2}*>(_length);\n'.format(t, name, vtype))
            else:
                fp.write('{0}{1}{2} = new ArrayList<{3}*>(_length);\n'.format(t, typestr, name, vtype))
        else:
            fp.write('{0}{1}{2} = new ArrayList<{3}>(_arrLen);\n'.format(t, typestr, name, typestr))
        fp.write(t + 'for (int {0} = 0; {0} < _length; {0}++) {{\n'.format(it))
        genReaderVar(fp, v, vtype, 'obj'+it, False, isClass, "", t+tab)
        fp.write(t + tab + '{0}->push_back({1});\n'.format(name, 'obj'+it))
        fp.write(t + '}\n')
    elif vtype == 'int':
        fp.write('{0}{1}{2} = buf.readInt32();\n'.format(t, typestr, name))
    elif vtype == 'float':
        fp.write('{0}{1}{2} = buf.readFloat();\n'.format(t, typestr, name))
    elif vtype == 'String':
        fp.write('{0}{1}{2} = buf.readUTF();\n'.format(t, typestr, name))
    elif vtype == 'short':
        fp.write('{0}{1}{2} = buf.readInt16();\n'.format(t, typestr, name))
    elif vtype == 'byte':
        fp.write('{0}{1}{2} = buf.readChar8();\n'.format(t, typestr, name))
    elif vtype == 'long':
        fp.write('{0}{1}{2} = buf.readInt64();\n'.format(t, typestr, name))
    elif vtype == 'bool':
        fp.write('{0}{1}{2} = buf.readBool();\n'.format(t, typestr, name))
    elif isClass:
        c = None
        for c1 in cfg['classes']:
            if c1['name'] == vtype:
                c = c1
                break
        fp.write('{0}{1}{2} = new {3}();\n'.format(t, typestr, name, vtype))
        for v1 in c['vars']:
            genReaderVar(fp, v1, v1['type'], name+'->'+v1['name'], v1['isArray'], v1['isClass'], v1['arrLenType'], t)
        

def genParserCpp():
    global iterIdx
    global lenDefined
    
    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        filepath = os.path.join(output, parser + g['name'] + '.cpp')
        fp = open(filepath, 'w')
        fp.write('#include "{0}.h"\n'
                 '#include "{1}.h"\n'
                 '\n'
                 '{0}::{0}(): handler(new {1}()) {{\n'
                 '}}\n'
                 '\n'
                 '{0}::~{0}() {{\n'
                 '    delete handler;\n'
                 '}}\n'
                 .format(
                     parser,
                     handler
                     ))

        for m in g['msgs']:
            if m['type'] == 'CS':
                continue
            fp.write('\n')
            fp.write('void {0}::parser_{1}_{2}(iobuf& buf) {{\n'
                     .format(
                         parser,
                         g['name'],
                         m['name']
                         ))
            for v in m['vars']:
                iterIdx = 0
                lenDefined = False
                genReaderVar(fp, v, v['type'], v['name'], v['isArray'], v['isClass'], v['arrLenType'], tab)

            #if len(m['vars']) > 0:
            #    fp.write('\n')

            isHasClass = hasClass(m)
            if isHasClass:
                fp.write(tab + 'bool saved = ')
            else:
                fp.write(tab)
                
            fp.write('handler->handler_{0}_{1}({2});\n'
                     .format(
                         g['name'],
                         m['name'],
                         genVarNameList(m)
                         ))
            
            if isHasClass:
                fp.write('    if (!saved) {{\n'
                         '{0}'
                         '    }}\n'.format(genCleanCode(m, tab+tab)))
            
            fp.write('}\n')
            
        fp.close()
            
#-------------------------------------------------------------------------------
#gen code: classes

def genClassH():
    l = getCppClassNames()
    fp = open(os.path.join(output, classes + '.h'), 'w')
    fp.write('#ifndef _{0}_H_\n'
             '#define _{0}_H_\n'
             '\n'
             '#include <gstl.h>\n'
             .format(classes.upper()))

    for c in cfg['classes']:
        if c['name'] in l:
            fp.write('\n')
            if c['comment'] != '':
                fp.write('//' + Comment(c) + '\n')
            fp.write('class {0} {{\n'
                     'public:\n'.format(c['name']))
            
            genVarDeclare(fp, c)

            fp.write('\n'
                     '    {0}();\n'
                     '    virtual ~{0};\n'
                     '}};\n'
                     .format(c['name']))

    fp.write('#endif /* _{0}_H_ */'.format(classes.upper()))
    fp.close()

def genClassCpp():
    l = getCppClassNames()
    fp = open(os.path.join(output, classes + '.cpp'), 'w')
    fp.write('#include "{0}.h"\n'.format(classes))
    
    for c in cfg['classes']:
        if c['name'] in l:
            fp.write('\n')
            fp.write('{0}::{0}()\n'
                     '{1}'
                     '{{}}\n'
                     '\n'
                     '{0}::~{0}() {{\n'
                     '{2}'
                     '}}\n'
                     .format(
                         c['name'],
                         genClassConstruct(c),
                         genClassDestruct(c)
                         ))
    fp.close()

#-------------------------------------------------------------------------------
#gen code: classes

def genSenderH():
    fp = open(os.path.join(output, sender + '.h'), 'w')
    fp.write('#ifndef _{0}_H_\n'
             '#define _{0}_H_\n'
             '\n'
             '#include <gstl.h>\n'
             '\n'
             'class {1} {{\n'
             'public:\n'
             .format(
                 sender.upper(),
                 sender
                 ))

    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        for m in g['msgs']:
            if m['type'] == 'SC':
                continue
            fp.write('\n');
            genMsgComment(fp, m, tab)
            fp.write('    static void send_{0}_{1}({2});\n'
                     .format(
                         g['name'],
                         m['name'],
                         genVarList(m)
                         ))
    
    fp.write('}};\n'
             '\n'
             '#endif /* _{0}_H_ */'.format(sender.upper()))
    fp.close()

def genWriteVar(fp, vtype, name, isArr, isClass, arrLenType, t):
    global iterIdx
    if isArr:
        it = iters[iterIdx]
        iterIdx += 1
        
        fp.write('{0}buf.{4}({1}.size());\n'
                 '{0}for (int {2} = 0; {2} < {1}.size(); {2}++) {{\n'
                 '{0}    {3} tmp = {1}[{2}];\n'
                 .format(t, name, it, vtype,
                         arrLenType=='int' and 'writeInt32' or 'writeInt16'))
        writeVar(fp, vtype, "tmp", False, isClass, t+tab, "")
        fp.write(t + '}\n')
    elif vtype == 'int':
        fp.write(t + 'buf.writeInt32(' + name + ');\n')
    elif vtype == 'float':
        fp.write(t + 'buf.writeFloat(' + name + ');\n')
    elif vtype == 'String':
        fp.write(t + 'buf.writeUTF(' + name + ');\n')
    elif vtype == 'short':
        fp.write(t + 'buf.writeInt16(' + name + ');\n')
    elif vtype == 'byte':
        fp.write(t + 'buf.writeChar8(' + name + ');\n')
    elif vtype == 'long':
        fp.write(t + 'buf.writeInt64(' + name + ');\n')
    elif vtype == 'bool':
        fp.write(t + 'buf.writeBool(' + name + ');\n')
    elif isClass:
        c = getClass(vtype)
        for v in c['vars']:
            genWriteVar(fp, v['type'], name+'.'+v['name'], v['isArray'], v['isClass'], v['arrLenType'], t)

def genSenderCpp():
    fp = open(os.path.join(output, sender + '.cpp'), 'w')
    
    fp.write('#include "{0}.h"\n'
             '#include "GameMain.h"\n'.format(sender))

    for g in cfg['groups']:
        if g['codeType'] != CODE_TYPE_CPP:
            continue
        for m in g['msgs']:
            if m['type'] == 'SC':
                continue
            fp.write('\n')
            fp.write('void {0}::send_{1}_{2}({3}) {{\n'
                     '    iobuf buf;\n'
                     '    buf.writeInt16({4});\n'
                     .format(
                         sender,
                         g['name'],
                         m['name'],
                         genVarList(m),
                         m['id']
                         ))
            
            for v in m['vars']:
                genWriteVar(fp, v['type'], v['name'], v['isArray'], v['isClass'], v['arrLenType'], tab)
                
            fp.write('    GameMain::getInstance()->sendTcpData(buf);\n'
                     '}\n')
    fp.close()

#-------------------------------------------------------------------------------
#run

genDispatchH()
genDispatchCpp()
genHandlerH()
genHandlerCpp()
genParserH()
genParserCpp()
genClassH()
genClassCpp()
genSenderH()
genSenderCpp()













    
