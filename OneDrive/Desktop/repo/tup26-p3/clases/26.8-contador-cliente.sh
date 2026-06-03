#!/bin/sh

clear
echo "=== Conectando a http://localhost:5001 (curl) ==="
base_url="http://localhost:5001/contador"

printf "\nEstado inicial: \n → "
curl -s "$base_url"
echo

printf "\nIncrementar 1:  \n → "
curl -s -X POST "$base_url"
echo

printf "\nIncrementar 2: \n → "
curl -s -X POST "$base_url"
echo

printf "\nEstado contador: \n → "
curl -s "$base_url"
echo

printf "\nBorrar contador: \n → "
curl -s -X DELETE "$base_url"
echo

printf "\nEstado final: \n → "
curl -s "$base_url"
echo
