# build.py
#
# This script combines the individual module folders into a single structure
# for Space Engineers to load (and a bunch of other useful deploy tasks)

import datetime, errno, logging, os.path, psutil, shutil, stat, subprocess, sys, time
from subprocess import Popen, PIPE

buildDir = os.getcwd()
scriptDir = os.path.dirname(os.path.realpath(sys.argv[0]))

logging.basicConfig(filename = scriptDir + r"\build.log", filemode = 'w', format = '%(asctime)s %(levelname)s: %(message)s', level = logging.DEBUG)

# script directories
buildIni = scriptDir + "\\build.ini"
startDir = os.path.split(scriptDir)[0]
cSharp = startDir + "/Scripts/"
modules = []
ignoreDirs = [ "bin", "obj", "Properties" ] # these are case-sensitive

# paths files are moved to
finalDir = os.getenv('APPDATA') + '\SpaceEngineers\Mods\ARMS'

# in case build.ini is missing variables
GitExe = os.devnull
SpaceEngineers = os.devnull
Zip7 = os.devnull


def createDir(l_dir):
	if not os.path.exists(l_dir):
		#logging.info ("making: "+l_dir)
		os.makedirs(l_dir)


def eraseDir(l_dir):
	if os.path.isdir(l_dir):
		logging.info ("deleting: "+l_dir)
		shutil.rmtree(l_dir)


# method that takes a module name and moves the files
def archiveScripts(l_source):
	logging.info("Archiving scripts from " + l_source)
	l_sourceDir = cSharp + l_source
	l_archiveDir = startDir + "\Archive\\" + l_source

	for path, dirs, files in os.walk(l_sourceDir):
		for ignore in ignoreDirs:
			if (ignore in dirs):
				dirs.remove(ignore)

		os.chdir(path)

		for file in files:
			if not file.lower().endswith(".cs"):
				continue

			# for archive, add date and time to file name
			createDir(l_archiveDir)
			d = datetime.datetime.fromtimestamp(os.path.getmtime(file))
			formated = str(d.year)+"-"+str(d.month).zfill(2)+"-"+str(d.day).zfill(2)+"_"+str(d.hour).zfill(2)+"-"+str(d.minute).zfill(2)+"_"+file
			archive = l_archiveDir +"\\"+formated
			try:
				os.chmod(archive, stat.S_IWRITE)
			except OSError:
				pass
			shutil.copyfile(file, archive)
			os.chmod(archive, stat.S_IREAD)


def copyWithExtension(l_from, l_to, l_ext, log):
	# delete orphan files
	for path, dirs, files, in os.walk(l_to):
		for file in files:
			source = path.replace(l_to, l_from) + '/' + file
			if not os.path.isfile(source):
				logging.info ("\tdeleting orphan file: " + file)
				os.remove(path + '/' + file)
	
	l_ext = l_ext.lower()
	for path, dirs, files, in os.walk(l_from):
		for dir in dirs:
			if dir == 'obj':
				dirs.remove(dir)
				
		for file in files:
			if file.lower().endswith(l_ext):
				target = path.replace(l_from, l_to)
				sourceFile = path + '/' + file
				if os.path.isdir(target):
					targetFile = target + '/' + file
					if (os.path.exists(targetFile) and os.path.getmtime(targetFile) == os.path.getmtime(sourceFile)):
						continue
				else:
					createDir(target)
				if log:
					logging.info ("Copying file: " + file)
				shutil.copy2(sourceFile, target)


def run_sepl(exe_path, json_config_path, build_dir, publish=False):
	command_parts = [exe_path, json_config_path, 'publish={}'.format(publish)]
	print('Running SEPL command:', command_parts)
	proc = Popen(command_parts, stdout=PIPE, stderr=PIPE, cwd=build_dir)
	(stdout, stderr) = proc.communicate()
	if proc.returncode == 0:
		print("SEPL succeeded.\n", stdout.decode('ascii'))
	else:
		raise ValueError('SEPL error:\n', stderr.decode('ascii'))


exec(open(buildIni).read())

if (not os.path.exists(SpaceEngineers)):
	logging.info ("You must set the path to SpaceEngineers in build.ini")
	sys.exit(11)

if (len(sys.argv) < 2):
	logging.error ("ERROR: Build configuration not specified")
	sys.exit(12)

exec(open(scriptDir + r"\find-git.py").read())

for process in psutil.process_iter():
	if process.name() == "SpaceEngineers.exe" or process.name() == "SpaceEngineersDedicated.exe" or process.name() == "LoadARMS.exe":
		logging.info("Killing process: " + process.name())
		process.kill()
		try:
			while process.status() != psutil.STATUS_DEAD:
				time.sleep(1)
		except psutil.NoSuchProcess:
			pass

build = sys.argv[1]
logging.info("Build is " + build)
#source = cSharp + 'bin/x64/' + build + '/ARMS.dll'
#if (not os.path.exists(source)):
#	logging.error("Build not found")
#	sys.exit(13)

#target = SpaceEngineers + '/Bin64'
#if (not os.path.exists(target)):
#	logging.error("Not path to Space Engineers: " + SpaceEngineers)
#	sys.exit(14)
#shutil.copy2(source, target)
#logging.info("Copied dll to " + target)

#target += '/ARMS - Release Notes.txt'
#notes = "Unoffical build: " + sys.argv[1] + "\nBuilt: " + datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S") + "\n"
#notes += "Commit: " + gitCommit + "\n"

#file = open(target, "w")
#file.write(notes)
#file.close()

#target = SpaceEngineers + '/DedicatedServer64'
#if (not os.path.exists(target)):
#	logging.error("Not path to Space Engineers: " + SpaceEngineers)
#	sys.exit(15)
#shutil.copy2(source, target)
#logging.info("Copied dll to " + target)

#target += '/ARMS - Release Notes.txt'
#file = open(target, "w")
#file.write(notes)
#file.close()

createDir(finalDir)

# erase old data
eraseDir(finalDir + '\\Data')

# copy data, models, and textures
copyWithExtension(startDir + '/Audio/', finalDir + '/Audio/', '.xwm', True)
copyWithExtension(startDir + '/Data/', finalDir + '/Data/', '.sbc', True)
copyWithExtension(startDir + '/Models/', finalDir + '/Models/', '.mwm', True)
copyWithExtension(startDir + '/Textures/', finalDir + '/Textures/', '.dds', True)
copyWithExtension(startDir + '/Scripts/SteamShipped/', finalDir + '/Data/Scripts/SteamShipped/', '.cs', True)

if "no-script" in str.lower(build):
	SpaceBin = SpaceEngineers + '\Bin64'
	SpaceEngineers = SpaceBin + '\SpaceEngineers.exe'
	os.system('start /D ' + SpaceBin + ' cmd /c"' + SpaceEngineers)
	sys.exit()
	
# get modules
os.chdir(startDir + '/Scripts/')
for file in os.listdir(startDir + '/Scripts/'):
	if file[0] == '.' or file == "Programmable":
		continue
	if (file in ignoreDirs):
		continue
	if os.path.isdir(file):
		modules.append(file)

# build scripts
for module in modules[:]:
	archiveScripts(module)

#pathPublish = os.path.split(startDir)[0] + "\\PublishARMS\\PublishARMS\\bin\\x64\\Release\\PublishARMS.exe"
#if "release" in str.lower(build) and os.path.isfile(pathPublish):
#	git tests moved to Publisher.cs
#	os.system('start /wait cmd /c "' + pathPublish + '" ' + build)
#else:

run_sepl(
	SpaceEngineers + '\\SpaceEngineersPluginLoader\\PluginManager.exe',
	startDir + '\\.build\\plugin.json',
	buildDir,
	("release" in str.lower(build))
)

SpaceBin = SpaceEngineers + '\Bin64'
if not "release" in str.lower(build):
	SpaceEngineers = SpaceBin + '\SpaceEngineers.exe'
	os.system('start /D "' + SpaceBin + '" cmd /c "' + SpaceEngineers + '"')

#    Pack Archive

os.chdir(startDir)

if not os.path.exists(Zip7):
	logging.info('\nNot running 7-Zip')
	sys.exit()

size = 0
for path, dirs, files in os.walk('Archive'):
	for f in files:
		fp = os.path.join(path, f)
		size += os.path.getsize(fp)
if (size < 10000000):
	sys.exit()

logging.info("\n7-Zip running")

cmd = [Zip7, 'u', 'Archive.7z', 'Archive']
process = subprocess.Popen(cmd, stdout = open(os.devnull, 'wb'))
process.wait()

if process.returncode != 0:
	logging.error("\n7-Zip failed\n")
	sys.exit(process.returncode)

logging.info("7-Zip finished\n")

# copied from http://stackoverflow.com/questions/1213706/what-user-do-python-scripts-run-as-in-windows/1214935#1214935
def handleRemoveReadonly(func, path, exc):
	excvalue = exc[1]
	if func in (os.rmdir, os.remove) and excvalue.errno == errno.EACCES:
		os.chmod(path, stat.S_IRWXU| stat.S_IRWXG| stat.S_IRWXO) # 0777
		func(path)
	else:
		raise

shutil.rmtree('Archive', ignore_errors=False, onerror=handleRemoveReadonly)
