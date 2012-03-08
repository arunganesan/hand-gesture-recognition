#! /usr/bin/env python

import numpy, gzip, random, cPickle, glob, os, re

def rint(minn, maxx): return random.randint(minn, maxx)

def make_features():
  max_offset = 1000
  min_offset = 10

  max_threshold = 100
  min_threshold = -100

  num_thresholds = 50
  num_features = 1000
  
  features = numpy.ndarray(shape=(num_features, 4 + num_thresholds))

  for i in range(num_features): 
    features[i,:] = [rint(min_offset, max_offset), rint(min_offset, max_offset), \
                     rint(min_offset, max_offset), rint(min_offset, max_offset)] + \
                    [random.randint(min_threshold, max_threshold) for j in range(num_thresholds)]
  
  return features

def extract_features(features, image):
  '''
  If an offset pixel lies on the background or outside the bounds of the image,
  the depth probe d_1(x') is given a large positive constant value
  '''
  out_of_bounds = 1000
  
  width = 640
  height = 480
  num_features = features.shape[0]
  
  image_features = numpy.ndarray(shape=(width, height, num_features))
  
  for y in range(height):
    for x in range(width):
      for idx, feature in enumerate(features):
        d = float(image[x, y, 0])
        u = (int(features[0]/d), int(features[1]/d))
        if u[0] < 0 or u[0] >= width or u[1] < 0 or u[1] >= height: d_u = out_of_bounds
        else: d_u = image[x + u[0], y + u[1], 0]
        
        v = (int(features[2]/d), int(features[3]/d))
        if v[0] < 0 or v[0] >= width or v[1] < 0 or v[1] >= height: d_v = out_of_bounds
        else: d_v = image[x + v[0], y + v[1], 0]
        
        image_features[x, y, idx] = d_u - d_v
  
  return image_features

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
