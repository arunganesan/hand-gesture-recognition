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
  
  # Open depth image, and assign label
  depth_file = open('%s/%s/%s_depth.txt.gz' % (samples_dir, label, filename), 'r').readlines()[0]
  label_file = open('%s/%s/%s_processed.txt.gz' % (samples_dir, label, filename), 'r').readlines()[0]
  
  depth = numpy.ndarray(shape=(640,480))
  for i in range(len(depth_file.split(' '))):
    x = i % 640
    y = int(i/640)
    depth[x,y] = float(depth_file.split(' ')[i])
    

  
  sample_num = 2000  
  points = [Pixel(i,i,i,i) for i in range(sample_num)]
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

