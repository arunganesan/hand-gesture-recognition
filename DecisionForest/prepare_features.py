#! /usr/bin/env python

import numpy, gzip, random

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

if __name__ == '__main__':
  features = make_features()
