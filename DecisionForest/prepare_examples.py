#! /usr/bin/env python

import numpy, os, glob, re, cPickle, random

'''
A random subset of 2000 example pixels from each image is chosen to ensure a 
roughly even distribution across body parts
'''


processed_samples_dir = 'processed_samples'
samples_dir = 'samples'

class Pixel:
  def __init__(self, x, y, z, label):
    self.x = x
    self.y = y
    self.z = z
    self.label = label


def load_sample(filename, label):
  # For each label, load up the file, sample 2000 pixels, and save their 
  # x,y,z location along with the label.
  sample_num = 2000
  points = [Pixel(i,i,i,i) for i in range(2000)]
  return random.sample(points, sample_num)

if __name__ == '__main__':
  labels = os.listdir(samples_dir)
  for label in labels:
    samples = glob.glob('%s/%s/*_processed.txt.gz' % (samples_dir, label))
    
    for sample in samples:
      m = re.match('(.*)_.*\.txt\.gz', os.path.basename(sample))
      processed = load_sample(m.group(1), label)
      
      savefilename = processed_samples_dir + '/' + m.group(1) + '.obj'
      savefile = open(savefilename, 'w')
      cPickle.dump(processed, savefile)
      savefile.close()

