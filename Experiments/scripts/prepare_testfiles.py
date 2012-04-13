#! /usr/bin/env python
import glob, random, os, subprocess

''' 
This script should only be run once before the start of the
experiments. It divides up the files from the source directory
into tests, and different sized subsets for the training.
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


# Gets a subset of `files` sampled evenly from the gestures
def get_subset (files, gestures, num_files):
  gesture_id = 0
  sampled_files = {}
  gesture_counts = {}

  for gesture in gestures:
    sampled_files[gesture] = []
    gesture_counts[gesture] = 0
    random.shuffle(files[gesture])
  
  while (num_files > 0):
    gesture = gestures[gesture_id]
    file_sample = gesture_counts[gesture]
    sampled_files[gesture].append(files[gesture][file_sample])
    gesture_counts[gesture] += 1
    gesture_id = (gesture_id + 1) % len(gestures)
    num_files -= 1
  
  return sampled_files


if __name__ == '__main__':
  gestures = 'OpenHand CloseHand Fist One'.split()
  source_suffix = 'Default'
  num_tests = 52
  num_train = [10] + range(50, 351, 50)
  test_files = {}
  files = {}
  
  for gesture in gestures:
      files[gesture] = glob.glob(gesture + source_suffix + '/*.txt')
  
  #print files
  print 'Generating test subsets'
  test_files = get_subset(files, gestures, num_tests)
  copy_files(test_files, 'test')
  for gesture in gestures:
    for filename in test_files[gesture]:
      files[gesture].remove(filename)
  
  # Repeat for all training sizes
  for size in num_train:
    print 'Generating %s-sized subsets' % str(size)
    subset = get_subset(files, gestures, size)
    copy_files(subset, str(size))

