#! /usr/bin/env python

from sklearn.cross_validation import cross_val_score
from sklearn.datasets import make_blobs
from sklearn.ensemble import RandomForestClassifier

X, y = make_blobs(n_samples=100, n_features=2000, centers=100, random_state=0)
print X.shape

#clf = RandomForestClassifier(n_estimators=3, max_depth=20, random_state=0)
#scores = cross_val_score(clf, X, y, cv=2)
#scores.mean()
