#! /usr/bin/env python

import numpy, os, glob, re, cPickle, random, gzip

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
  depth_file = gzip.open('%s/%s/%s_depth.txt.gz' % (samples_dir, label, filename), 'r').readlines()[0]
  label_file = gzip.open('%s/%s/%s_processed.txt.gz' % (samples_dir, label, filename), 'r').readlines()[0]
  
  depth_linear = [int(val) for val in depth_file.split(' ')]
  label_linear = [int(val) for val in label_file.split(' ')]
  
  print 'Got depth map'
  depth = numpy.ndarray(shape=(640,480))
  for idx, val in enumerate(depth_linear):
    x = idx % 640
    y = int(idx/640)
    depth[x,y] = float(val)
  
  # Now get the classification of each pixel
  print 'Step 2'
  points = []
  for y in range(480):
    print y
    for x in range(640):
      # Get label
      if label_file[(y*640 + x)*4] == 255: pixel_class = label
      else: pixel_class = 'background'
      p = Pixel(x, y, depth[x,y], pixel_class)
      points.append(p)
  
  print 'Done'
  return points

if __name__ == '__main__':
  labels = os.listdir(samples_dir)
  for label in labels:
    print 'Processing ' + label
    
    samples = glob.glob('%s/%s/*_processed.txt.gz' % (samples_dir, label))
    
    for sample in samples:
      print '\t%s' % sample
      m = re.match('(.*)_.*\.txt\.gz', os.path.basename(sample))
      processed = load_sample(m.group(1), label)
      print 'Done processed'
      
      savefilename = processed_samples_dir + '/' + m.group(1) + '.obj.gz'
      print 'Saving...'
      savefile = gzip.open(savefilename, 'w')
      
      cPickle.dump(processed, savefile)
      print 'Saved.'
      savefile.close()

