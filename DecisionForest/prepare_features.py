#! /usr/bin/env python

import numpy, gzip, random, cPickle, glob, os, re

def make_features():
  max_offset = 1000
  min_offset = 10

  max_threshold = 100
  min_threshold = -100

  num_thresholds = 50
  num_features = 1000
  
  features = numpy.ndarray(shape=(num_features, 2 + num_thresholds))

  for i in range(num_features): 
    features[i,:] = [random.randint(min_offset, max_offset), \
                     random.randint(min_offset, max_offset)] + \
                    [random.randint(min_threshold, max_threshold) for j in range(num_thresholds)]
  
  return features

def extract_features(features, image):
  return features

if __name__ == '__main__':
  features = make_features()
  sf = gzip.open('features.obj', 'w')
  cPickle.dump(features, sf)
  sf.close()
  
  # Load pictures and calculate feature vectors
  images = glob.glob('processed_samples/*.gz')
  for image in images:
    m = re.match('(.*)\.obj\.gz', os.path.basename(image))
    filename = m.group(1)
    
    fv = extract_features(features, image)
    fv_file = gzip.open('feature_vectors/%s.obj.gz' % filename, 'w')
    cPickle.dump(fv, fv_file)
    fv_file.close()
    
    exit(1)
