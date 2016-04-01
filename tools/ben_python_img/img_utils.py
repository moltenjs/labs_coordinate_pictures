from ben_python_common import *

# field to store original title in. There's an exif tag OriginalRawFileName, but isn't shown in UI.
ExifFieldForOriginalTitle = "Copyright"
MarkerString = "__MARKAS__"

def readOption(key):
    fname = '../options.ini'
    if not files.exists(fname):
        raise RuntimeError('cannot locate options.ini file.')
    
    with open(fname, 'r') as f:
        for line in f:
            if line.startswith(key + '='):
                line = line.strip()
                return line[len(key + '='):]
    raise RuntimeError('key ' + key + ' not found in options.ini')

def getCwebpLocation():
    ret = readOption('FilepathWebp')
    assertTrue(ret.endswith(files.sep + 'cwebp.exe'))
    return ret
    
def getMozjpegLocation():
    ret = readOption('FilepathMozJpeg')
    assertTrue(ret.endswith(files.sep + 'cjpeg.exe'))
    return ret
    
def getExifToolLocation():
    ret = readOption('FilepathExifTool')
    assertTrue(ret.endswith(files.sep + 'exiftool.exe'))
    return ret

def getDwebpLocation():
    cwebpLocation = readOption('FilepathWebp')
    return files.join(files.getparent(cwebpLocation), 'dwebp.exe')
    
def getTempLocation():
    # will also be periodically deleted by coordinate_pictures
    import tempfile
    dir = files.join(tempfile.gettempdir(), 'test_labs_coordinate_pictures')
    if not files.exists(dir):
        files.makedirs(dir)
    return dir

def exiftool():
    return getExifToolLocation()

class PythonImgExifException(Exception):
    def __init__(self, *args):
        trace('PythonImgExifException created.')

def readExifField(filename, exifField):
    args = "{0}|-S|-{1}|{2}".format(exiftool(), ExifFieldForOriginalTitle, filename)
    args = args.split('|')
    ret, stdout, stderr = files.run(args, shell=False, throwOnFailure=PythonImgExifException)
    sres = stdout.strip()
    if sres:
        assertTrue(sres.startswith(exifField + ': '))
        return sres[len(exifField + ': '):]
    else:
        return ''

def setExifField(filename, exifField, value):
    # -m flag lets exiftool() ignores minor errors
    args = "{0}|-{1}={2}|-overwrite_original|-m|{3}".format(
        exiftool(), exifField, value, filename)
    args = args.split('|')
    files.run(args, shell=False, throwOnFailure=PythonImgExifException)

def readThumbnails(dirname, removeThumbnails, outputDir=None):
    if outputDir and not files.isdir(outputDir):
        files.makedirs(outputDir)
    assertTrue(files.isdir(dirname))
    for filename, short in files.listfiles(dirname):
        if filename.lower().endswith('.jpg'):
            if not removeThumbnails:
                outFile = files.join(outputDir, short)
                assertTrue(not files.exists(outFile), 'file already exists ' + outFile)
                args = "{0}|-b|-ThumbnailImage|{1}|>|{2}".format(
                    exiftool(), filename, outFile)
            else:
                args = "{0}|-PreviewImage=|{1}".format(exiftool(), filename)
            
            trace(short)
            ret, stdout, stderr = files.run(args.split('|'), shell=True, throwOnFailure=PythonImgExifException)
            trace(stdout)
            
def readOriginalFilename(filename):
    return readExifField(filename, ExifFieldForOriginalTitle)

def stampJpgWithOriginalFilename(shortFilename, filename):
    setExifField(filename, ExifFieldForOriginalTitle, shortFilename)
    
def removeResolutionTags(filename):
    # -m flag lets exiftool() ignores minor errors
    # cannot set to empty string, so follow what Python PIL/Pillow do and set to 1
    args = "{0}|-XResolution=1|-YResolution=1|-ResolutionUnit=None|-overwrite_original|-m|{1}".format(
        exiftool(), filename)
    args = args.split('|')
    ret, stdout, stderr = files.run(args, shell=False, throwOnFailure=PythonImgExifException)

def removeAllExifTags(filename):
    args = "{0}|-all=|-overwrite_original|{1}".format(
        exiftool(), filename)
    files.run(args, shell=False)
    
def removeAllExifTagsInDirectory(dirname):
    assertTrue(files.isdir(dirname))
    if getInputBool('remove all tags?'):
        for filename, short in files.listfiles(dirname):
            if filename.lower().endswith('.jpg'):
                removeAllExifTags(filename)

def transferMostUsefulExifTags(src, dest):
    '''When resizing a jpg file, all exif data is lost, since we convert to raw as an intermediate step.
    Also, if we copied all exif metadata, modern cameras add quite a bit of exif+xmp, at least 15kb.
    So, use an allow-list to copy over only the most useful exif tags.
    There's a list of fields at http://www.sno.phy.queensu.ca/~phil/exiftool()/TagNames/EXIF.html'''
    
    # to get everything except xmp and thumbnail, use
    # [exiftool(), '-tagsFromFile', src, '-XMP:All=', '-ThumbnailImage=', '-m', dest]
    
    tagsWanted = [
        'DateTimeOriginal',
        'CreateDate',  # aka DateTimeDigitized
        'Make',
        'Model',
        'LensInfo',
        'FNumber',
        'ExposureTime',
        'ISO',  # aka ISOSpeedRatings
        'ExposureCompensation',  # aka ExposureBiasValue
        'FocalLength',
        'ApertureValue',
        'MaxApertureValue',
        'MeteringMode',
        'Flash',
        'FlashEnergy',
        'FocalLengthIn35mmFormat',  # aka FocalLengthIn35mmFilm
        'DigitalZoomRatio']
        
    cmd = [exiftool(), '-tagsFromFile', src]
    for tag in tagsWanted:
        cmd.append('-' + tag)
    cmd.append('-overwrite_original')
    cmd.append('-m')  # ignore minor errors
    cmd.append(dest)
    files.run(cmd, shell=False, throwOnFailure=PythonImgExifException)
    
def getMarkFromFilename(pathAndCategory):
    '''returns tuple pathWithoutCategory, category'''
    
    # check nothing in path has mark
    if (MarkerString in files.getparent(pathAndCategory)):
        raise ValueError('Directories should not have marker')

    parts = pathAndCategory.split(MarkerString)
    if len(parts) != 2:
        raise ValueError('Expected path to have exactly one marker')

    partsAfterMarker = parts[1].split('.')
    if (len(partsAfterMarker) != 2):
        raise ValueError('Parts after the marker shouldn\'t have another .')
    
    category = partsAfterMarker[0]
    pathWithoutCategory = parts[0] + "." + partsAfterMarker[1]
    return pathWithoutCategory, category