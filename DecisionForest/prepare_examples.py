#! /usr/bin/env python

import numpy, os, glob, re, cPickle, random, gzip

processed_samples_dir = 'processed_samples'
samples_dir = 'samples'


def load_sample(filename, label, label_idx):
  # Open depth image, and assign label
  depth_file = gzip.open('%s/%s/%s_depth.txt.gz' % (samples_dir, label, filename), 'r').readlines()[0]
  label_file = gzip.open('%s/%s/%s_processed.txt.gz' % (samples_dir, label, filename), 'r').readlines()[0]
  
  depth_linear = [int(val) for val in depth_file.split(' ')]
  label_linear = [int(val) for val in label_file.split(' ')]
  
  save_format = numpy.ndarray(shape=(640,480, 2))
  
  #random_sampling = random.sample(range(640*480), 2000)
  
  for y in range(480):
    for x in range(640):
      idx = y*640 + x
      if label_linear[idx*4] == 255: pixel_class = label_idx
      else: pixel_class = -1
      save_format[x,y,:] = [depth_linear[idx], pixel_class]
  
  return save_format

if __name__ == '__main__':
  labels = os.listdir(samples_dir)
  for label_idx, label in enumerate(labels):
    print 'Processing %d - %s' % (label_idx, label)
    
    samples = glob.glob('%s/%s/*_processed.txt.gz' % (samples_dir, label))
    
    for sample in samples:
      print '\t%s' % sample
      m = re.match('(.*)_.*\.txt\.gz', os.path.basename(sample))
      processed = load_sample(m.group(1), label, label_idx)
      savefilename = processed_samples_dir + '/' + m.group(1) + '.obj.gz'
      savefile = gzip.open(savefilename, 'w')
      
      cPickle.dump(processed, savefile)
      savefile.close()

