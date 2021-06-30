#!/usr/bin/env python
import sys

from setuptools import setup
from setuptools_rust import Binding, RustExtension

setup(
    name="pyqir",
    version="0.0.1",
    classifiers=[
        "License :: OSI Approved :: MIT License",
        "Development Status :: 3 - Alpha",
        "Intended Audience :: Developers",
        "Programming Language :: Python :: 3.6",
        "Programming Language :: Python :: 3.7",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python",
        "Programming Language :: Rust",
        "Operating System :: MacOS",
        "Operating System :: Microsoft :: Windows",
        "Operating System :: POSIX :: Linux",
    ],
    packages=["pyqir"],
    rust_extensions=[RustExtension("pyqir.pyqir", "Cargo.toml", debug=False)],
    include_package_data=True,
    zip_safe=False,
)