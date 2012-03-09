#! /usr/bin/env python

import numpy, gzip, random, cPickle, glob, os, re
import threading, math

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

def extract_features(features, image_filename):
  '''
  If an offset pixel lies on the background or outside the bounds of the image,
  the depth probe d_1(x') is given a large positive constant value
  '''
  out_of_bounds = 1000
  
  image_file = gzip.open(image_filename, 'r')
  image = cPickle.load(image_file)
  image_file.close()
  
  width = 640
  height = 480
  num_features = features.shape[0]
  
  num_samples = 2000
  samples = [(i % width, int(i / width)) for i in random.sample(range(width*height), num_samples)]
  image_features = numpy.ndarray(shape=(num_samples, num_features))
  
  for s_idx, (x, y) in enumerate(samples):
    for f_idx in range(num_features):
      feature = features[f_idx, 0:4]
      
      d = float(image[x, y, 0])
      
      if d == 0:
        image_features[s_idx, f_idx] = 0
        continue
      
      
      u = (int(feature[0]/d), int(feature[1]/d))
      (x_u, y_u) = (x + u[0], y + u[1])

      if x_u < 0 or x_u >= width or y_u < 0 or y_u >= height: d_u = out_of_bounds
      else: d_u = image[x_u, y_u, 0]
      
      v = (int(feature[2]/d), int(feature[3]/d))
      (x_v, y_v) = (x + v[0], y + v[1])
      if x_v < 0 or x_v >= width or y_v < 0 or y_v >= height: d_v = out_of_bounds
      else: d_v = image[x_v, y_v, 0]
      
      image_features[s_idx, f_idx] = d_u - d_v
      
  return image_features

def process_images(images, features):
  for image in images:
    m = re.match('(.*)\.obj\.gz', os.path.basename(image))
    filename = m.group(1)
    fv = extract_features(features, image)
    fv_file = gzip.open('feature_vectors/%s.obj.gz' % filename, 'w')
    cPickle.dump(fv, fv_file)
    fv_file.close()
    print filename
  

# Courtesy this hacker on StackOverflow: http://stackoverflow.com/questions/312443/how-do-you-split-a-list-into-evenly-sized-chunks-in-python
def chunks (l, n):
  for i in xrange(0, len(l), n):
    yield l[i:i + n]

if __name__ == '__main__':
  features = make_features()
  sf = gzip.open('features.obj', 'w')
  cPickle.dump(features, sf)
  sf.close()
  
  # Load pictures and calculate feature vectors
  images = glob.glob('processed_samples/*.gz')
 
  thread_count = 4
  chunk_size = int(math.ceil(float(len(images))/float(thread_count)))
  for chunk in chunks(images, chunk_size):
    print 'Starting thread'
    t = threading.Thread(target=process_images, args=(chunk, features))
    t.start()


  
   
