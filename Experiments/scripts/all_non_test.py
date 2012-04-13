#! /usr/bin/env python
import glob, random, os, subprocess

'''
This script collects all non-test images and puts them in 
one folder.
'''

# Creates the folder to copy the files into
def clear_folder (name):
  if not os.path.exists(name): os.mkdir(name)
  for filename in glob.glob('%s/*.txt' % name):
    subprocess.call(['rm', filename])

# Copies the files
def copy_files (subset_files, suffix):
  print 'Copying to %s' % suffix
  for gesture in gestures:
    folder = '%s.%s' % (gesture, suffix)
    clear_folder(folder)
    for filename in subset_files[gesture]:
      subprocess.call(['cp', filename, folder])

if __name__ == '__main__':
  gestures = 'OpenHand CloseHand Fist One'.split()
  source_suffix = 'Default'
  destination_suffix = 'large'
  files = {}
  
  # Get all files
  for gesture in gestures:
    files[gesture] = glob.glob(gesture + source_suffix + '/*.txt')
  
  # Exclude test files
  print 'Getting test file names'
  for gesture in gestures:
    for filename in glob.glob(gesture + '.test/*.txt'):
      modified_filename = gesture + source_suffix + '/' + \
                          os.path.basename(filename)
      files[gesture].remove(modified_filename)
  

  copy_files(files, destination_suffix)
