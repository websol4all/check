#!/bin/bash

git branch
git pull
git add --all .
echo "Please, enter commit:"

read B
git commit -m "$B"
git push