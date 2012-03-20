#! /usr/bin/env python
#from sklearn.datasets import load_svmlight_file
from svmlight_loader import load_svmlight_file
from sklearn.ensemble import RandomForestClassifier
from sklearn.cross_validation import cross_val_score 
import pickle

print 'Loading feature file...'
X, y = load_svmlight_file('FeatureVectorBlueDefault.txt')

print 'Training classifier...'
clf = RandomForestClassifier(n_estimators=3, criterion='entropy', max_depth=20)
clf.fit(X, y)

print 'Saving model...'
file = open('forest.model', 'w')
pickle.dump(clf, file)
file.close()
#scores = cross_val_score(clf, X, y)
#print scores.mean()
